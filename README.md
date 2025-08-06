# Meteorite Landings Application

## Overview

A full-stack application for viewing and analyzing NASA meteorite landing data. Built with clean architecture principles, this application provides a REST API backend and React TypeScript frontend for exploring meteorite landing patterns by year, classification, and location.

## Architecture

### Backend (.NET 8)
- **Clean Architecture** with Domain, Application, Infrastructure, and API layers
- **PostgreSQL** database with Entity Framework Core
- **NASA API Integration** with background data synchronization
- **Circuit Breaker & Retry Policies** for resilient external API calls
- **Memory Caching** with optimized cache key generation
- **Health Checks** for monitoring database and external API connectivity
- **Comprehensive Error Handling** with structured logging

### Frontend (React + TypeScript)
- **React 19** with TypeScript for type safety
- **Vite** for fast development and building
- **Debounced API calls** to reduce server load
- **Error boundaries** and robust error handling
- **Responsive UI** with sortable data tables

## Features

### Data Management
- 🚀 **Real-time Sync**: Background service synchronizes with NASA API
- 🔄 **Smart Caching**: Intelligent caching with SHA256-based cache keys
- 📊 **Data Validation**: Comprehensive validation at all layers
- 🔍 **Advanced Filtering**: Filter by year range, meteorite class, and name

### Performance & Reliability
- ⚡ **Optimized Queries**: Database indexes for fast filtering
- 🛡️ **Circuit Breaker**: Prevents cascading failures from external API
- 🔁 **Retry Policies**: Exponential backoff for transient failures  
- 🏥 **Health Monitoring**: Built-in health checks for dependencies

### Security & Quality
- 🔒 **SQL Injection Prevention**: Parameterized queries with EF.Functions
- ✅ **Input Validation**: Multi-layer validation with detailed error messages
- 🚫 **Concurrency Protection**: Prevents overlapping sync operations
- 📝 **Structured Logging**: Comprehensive logging for debugging and monitoring

## Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- PostgreSQL 15+
- Docker (optional)

### Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd MeteoriteLandings
   ```

2. **Setup Database**
   ```bash
   # Update connection string in appsettings.Development.json
   dotnet ef database update --project MeteoriteLandings.Infrastructure
   ```

3. **Run Backend**
   ```bash
   dotnet run --project MeteoriteLandings.API
   ```

4. **Run Frontend**
   ```bash
   cd frontend-react-ts
   npm install
   npm run dev
   ```

### Docker Setup

```bash
# Copy environment template
cp .env.example .env

# Start all services
docker-compose up -d
```

## API Documentation

### Endpoints

#### GET /api/meteorites
Retrieve filtered and grouped meteorite landing data.

**Query Parameters:**
- `startYear` (int, optional): Filter landings from this year onwards
- `endYear` (int, optional): Filter landings up to this year
- `recClass` (string, optional): Filter by meteorite classification
- `nameContains` (string, optional): Filter by name substring
- `sortBy` (string, optional): Sort by 'year', 'count', 'totalmass' (default: 'year')
- `sortOrder` (string, optional): 'asc' or 'desc' (default: 'asc')

**Response:**
```json
[
  {
    "year": 2020,
    "count": 45,
    "totalMass": 15432.50
  }
]
```

#### GET /api/meteorites/recclasses
Retrieve unique meteorite classifications.

**Response:**
```json
["H5", "L6", "LL3", "Eucrite"]
```

### Health Checks

- `GET /health` - Complete health report
- `GET /health/ready` - Readiness probe (database + NASA API)
- `GET /health/live` - Liveness probe

## Configuration

### Application Settings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=MeteoriteLandings;Username=user;Password=pass"
  },
  "DataSyncService": {
    "SyncIntervalMinutes": 60
  },
  "ExternalApis": {
    "NasaApi": {
      "BaseUrl": "https://data.nasa.gov/resource/y77d-th95.json",
      "Timeout": "00:00:30",
      "RetryCount": 3,
      "RetryDelay": "00:00:02",
      "CircuitBreakerThreshold": 5,
      "CircuitBreakerDuration": "00:01:00"
    }
  }
}
```

### Environment Variables

```bash
# Database
POSTGRES_DB=meteoritedb
POSTGRES_USER=meteors
POSTGRES_PASSWORD=secure_password

# API
API_HOST_PORT=7000
API_CONTAINER_PORT=8080
API_CORS_ORIGINS=http://localhost:3000

# Frontend
FRONTEND_HOST_PORT=3000
FRONTEND_CONTAINER_PORT=80
VITE_API_BASE_URL=http://localhost:7000

# Background Service
DataSyncService__SyncIntervalHours=1
```

## Development

### Project Structure

```
MeteoriteLandings/
├── MeteoriteLandings.API/          # Web API layer
├── MeteoriteLandings.Application/  # Business logic layer
├── MeteoriteLandings.Domain/       # Domain entities
├── MeteoriteLandings.Infrastructure/ # Data access & external services
├── frontend-react-ts/              # React frontend
├── docker-compose.yml              # Docker orchestration
└── README.md                       # This file
```

### Key Improvements Made

#### Security Enhancements
- ✅ Fixed SQL injection vulnerabilities using `EF.Functions.ILike`
- ✅ Added comprehensive input validation with RegEx patterns
- ✅ Implemented secure cache key generation using SHA256

#### Performance Optimizations
- ✅ Enhanced database query performance with proper indexing
- ✅ Optimized frontend with debounced API calls
- ✅ Improved caching mechanism with proper expiration policies
- ✅ Reduced memory allocations in React components

#### Reliability Improvements
- ✅ Added concurrency protection for background sync service
- ✅ Implemented proper error handling throughout the stack
- ✅ Added data validation at sync level
- ✅ Enhanced logging and monitoring capabilities

### Testing

```bash
# Run backend tests (when added)
dotnet test

# Run frontend tests
cd frontend-react-ts
npm test

# Run linting
npm run lint
```

### Deployment

See [PRODUCTION_READY_GUIDE.md](PRODUCTION_READY_GUIDE.md) for detailed production deployment instructions.

## Monitoring

### Key Metrics to Monitor

- **Health Check Response Times**: Database < 50ms, NASA API < 500ms
- **Cache Hit Ratio**: Should be > 80% for repeated queries
- **Circuit Breaker State**: Monitor for frequent state changes
- **Sync Service Performance**: Track sync duration and error rates
- **API Response Times**: 95th percentile < 200ms for cached queries

### Logging

The application uses structured logging with the following levels:
- **Error**: Application errors, external API failures
- **Warning**: Validation failures, circuit breaker openings
- **Information**: Cache operations, sync status, health checks
- **Debug**: Query execution times, detailed flow tracing

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- NASA Open Data Portal for providing meteorite landing data
- .NET and React communities for excellent documentation
- Contributors and maintainers of open-source dependencies
