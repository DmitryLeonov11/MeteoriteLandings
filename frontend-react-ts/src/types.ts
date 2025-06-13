export type SortByOption = 'year' | 'count' | 'totalMass';
export type SortOrderOption = 'asc' | 'desc';

export interface MeteoriteLandingGroupedByYearDto {
    year: number;
    count: number;
    totalMass: number;
}

export interface MeteoriteLandingFilterDto {
    startYear?: number;
    endYear?: number;
    recClass?: string;
    nameContains?: string;
    sortBy?: SortByOption;
    sortOrder?: SortOrderOption;
}

export interface ApiErrors {
    [key: string]: string[];
}