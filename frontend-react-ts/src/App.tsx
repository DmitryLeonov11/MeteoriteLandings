import { useState, useEffect, useCallback, useMemo } from 'react';
import type {
    MeteoriteLandingGroupedByYearDto,
    MeteoriteLandingFilterDto,
    ApiErrors,
    SortByOption,
    SortOrderOption
} from './types';

import './App.css';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:7000';

const MIN_YEAR = 1800;
const MAX_YEAR = new Date().getFullYear();

// Generate years array once outside component to avoid recreation on each render
const YEARS_ARRAY = Array.from({ length: MAX_YEAR - MIN_YEAR + 1 }, (_, i) => MIN_YEAR + i).reverse();

function App() {
    const [landings, setLandings] = useState<MeteoriteLandingGroupedByYearDto[]>([]);
    const [loading, setLoading] = useState<boolean>(false);
    const [apiErrors, setApiErrors] = useState<ApiErrors | null>(null);
    const [generalError, setGeneralError] = useState<string | null>(null);
    const [filter, setFilter] = useState<MeteoriteLandingFilterDto>({
        sortBy: 'year',
        sortOrder: 'asc',
        startYear: undefined,
        endYear: undefined,
        recClass: '',
        nameContains: ''
    });

    const [uniqueRecClasses, setUniqueRecClasses] = useState<string[]>([]);

    // Debounced fetch to avoid too many API calls
    const debouncedFetch = useMemo(() => {
        let timeoutId: NodeJS.Timeout;
        return (filterToUse: MeteoriteLandingFilterDto) => {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => {
                fetchData(filterToUse);
            }, 300);
        };
    }, []);

    const fetchData = useCallback(async (filterToUse: MeteoriteLandingFilterDto) => {
        setLoading(true);
        setApiErrors(null);
        setGeneralError(null);

        const params = new URLSearchParams();
        if (filterToUse.startYear) params.append('startYear', filterToUse.startYear.toString());
        if (filterToUse.endYear) params.append('endYear', filterToUse.endYear.toString());
        if (filterToUse.recClass && filterToUse.recClass !== '') params.append('recClass', filterToUse.recClass);
        if (filterToUse.nameContains?.trim()) params.append('nameContains', filterToUse.nameContains.trim());
        if (filterToUse.sortBy) params.append('sortBy', filterToUse.sortBy);
        if (filterToUse.sortOrder) params.append('sortOrder', filterToUse.sortOrder);

        try {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 30000); // 30 second timeout
            
            const response = await fetch(`${API_BASE_URL}/api/meteorites?${params.toString()}`, {
                signal: controller.signal,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                },
            });
            
            clearTimeout(timeoutId);

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ 
                    error: `HTTP ${response.status}: ${response.statusText}` 
                }));
                
                console.error("API Error Response:", errorData);

                if (errorData.errors) {
                    setApiErrors(errorData.errors as ApiErrors);
                    setGeneralError("Validation errors occurred. Please check the form.");
                } else if (errorData.error) {
                    setGeneralError(errorData.error);
                } else if (typeof errorData === 'string' || (Array.isArray(errorData) && errorData.every(item => typeof item === 'string'))) {
                    setGeneralError(Array.isArray(errorData) ? errorData.join(', ') : errorData);
                } else {
                    setGeneralError(errorData.title || `An unknown error occurred: ${response.status}`);
                }
            } else {
                const data = await response.json();
                setLandings(Array.isArray(data) ? data : []);
            }
        } catch (error) {
            if (error instanceof Error) {
                if (error.name === 'AbortError') {
                    setGeneralError('Request timed out. Please try again.');
                } else {
                    console.error("Fetch Error:", error);
                    setGeneralError(`Network error: ${error.message}`);
                }
            } else {
                setGeneralError('An unexpected error occurred.');
            }
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        const fetchRecClasses = async () => {
            try {
                const response = await fetch(`${API_BASE_URL}/api/meteorites/recclasses`);
                if (response.ok) {
                    const classes = await response.json();
                    setUniqueRecClasses(classes);
                } else {
                    console.error("Error fetching unique rec classes:", response.statusText);
                    setGeneralError(`Failed to load meteorite classes: ${response.statusText}`);
                }
            } catch (error) {
                console.error("Fetch error for rec classes:", error);
                setGeneralError(`Network error loading meteorite classes: ${error instanceof Error ? error.message : String(error)}`);
            }
        };
        fetchRecClasses();
    }, []);

    // Initial data fetch and filter changes
    useEffect(() => {
        fetchData(filter);
    }, []); // Only on mount

    // Use debounced fetch for filter changes except sorting
    useEffect(() => {
        const { sortBy, sortOrder, ...filterWithoutSort } = filter;
        const hasFilterValues = Object.values(filterWithoutSort).some(val => val !== undefined && val !== '');
        
        if (hasFilterValues) {
            debouncedFetch(filter);
        } else {
            // If no filters, fetch immediately
            fetchData(filter);
        }
    }, [filter.startYear, filter.endYear, filter.recClass, filter.nameContains, debouncedFetch]);

    // Immediate fetch for sorting changes
    useEffect(() => {
        fetchData(filter);
    }, [filter.sortBy, filter.sortOrder, fetchData]);

    // Use the pre-computed years array
    const years = YEARS_ARRAY;

    const handleSelectChange = useCallback((e: React.ChangeEvent<HTMLSelectElement>) => {
        const { name, value } = e.target;
        setFilter(prevFilter => ({
            ...prevFilter,
            [name]: value === '' ? undefined : (name === 'startYear' || name === 'endYear' ? parseInt(value, 10) : value)
        }));
    }, []);

    const handleFilterChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFilter(prevFilter => ({
            ...prevFilter,
            [name]: value
        }));
    }, []);

    const handleSort = useCallback((sortByOption: SortByOption) => {
        setFilter(prevFilter => {
            const newSortOrder: SortOrderOption = (prevFilter.sortBy === sortByOption && prevFilter.sortOrder === 'asc') ? 'desc' : 'asc';
            return {
                ...prevFilter,
                sortBy: sortByOption,
                sortOrder: newSortOrder
            };
        });
    }, []);

    const renderFieldErrors = (fieldName: string) => {
        if (apiErrors && apiErrors[fieldName]) {
            return (
                <ul className="error-messages">
                    {apiErrors[fieldName].map((msg, idx) => <li key={idx}>{msg}</li>)}
                </ul>
            );
        }
        return null;
    };

    return (
        <div className="App">
            <h1>Meteorite Landings</h1>

            <div className="filters">
                <div>
                    <label htmlFor="startYear">Start Year:</label>
                    <select
                        id="startYear"
                        name="startYear"
                        value={filter.startYear ?? ''}
                        onChange={handleSelectChange}
                    >
                        <option value="">Any</option>
                        {years.map(year => (
                            <option key={`start-${year}`} value={year}>{year}</option>
                        ))}
                    </select>
                    {renderFieldErrors("startYear")}
                </div>
                <div>
                    <label htmlFor="endYear">End Year:</label>
                    <select
                        id="endYear"
                        name="endYear"
                        value={filter.endYear ?? ''}
                        onChange={handleSelectChange}
                    >
                        <option value="">Any</option>
                        {years.map(year => (
                            <option key={`end-${year}`} value={year}>{year}</option>
                        ))}
                    </select>
                    {renderFieldErrors("endYear")}
                </div>
                <div>
                    <label htmlFor="recClass">Rec Class:</label>
                    <select
                        id="recClass"
                        name="recClass"
                        value={filter.recClass ?? ''}
                        onChange={handleSelectChange}
                    >
                        <option value="">Any</option>
                        {uniqueRecClasses.map(rc => (
                            <option key={rc} value={rc}>{rc}</option>
                        ))}
                    </select>
                    {renderFieldErrors("recClass")}
                </div>
                <div>
                    <label htmlFor="nameContains">Name Contains:</label>
                    <input
                        type="text"
                        id="nameContains"
                        name="nameContains"
                        value={filter.nameContains ?? ''}
                        onChange={handleFilterChange}
                    />
                    {renderFieldErrors("nameContains")}
                </div>

                <button onClick={() => fetchData(filter)} disabled={loading}>
                    {loading ? 'Loading...' : 'Apply Filters'}
                </button>
            </div>

            {generalError && <div className="general-error">{generalError}</div>}

            {loading && <p>Loading data...</p>}

            {!loading && landings.length === 0 && !generalError && <p>No data found for the selected filters.</p>}

            {!loading && landings.length > 0 && (
                <table>
                    <thead>
                    <tr>
                        <th onClick={() => handleSort('year')}>
                            Year {filter.sortBy === 'year' && (filter.sortOrder === 'asc' ? '▲' : '▼')}
                        </th>
                        <th onClick={() => handleSort('count')}>
                            Count {filter.sortBy === 'count' && (filter.sortOrder === 'asc' ? '▲' : '▼')}
                        </th>
                        <th onClick={() => handleSort('totalMass')}>
                            Total Mass {filter.sortBy === 'totalMass' && (filter.sortOrder === 'asc' ? '▲' : '▼')}
                        </th>
                    </tr>
                    </thead>
                    <tbody>
                    {landings.map((landing, index) => (
                        <tr key={index}>
                            <td>{landing.year}</td>
                            <td>{landing.count}</td>
                            <td>{landing.totalMass.toFixed(2)}</td>
                        </tr>
                    ))}
                    </tbody>
                </table>
            )}
        </div>
    );
}

export default App;
