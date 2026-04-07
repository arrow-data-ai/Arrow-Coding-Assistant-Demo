# Sample Task Manager API

A lightweight REST API for managing tasks, built with FastAPI and SQLAlchemy.

## Architecture

```
sample-app/
├── main.py              # FastAPI application entry point
├── models/
│   ├── database.py      # SQLAlchemy engine & session setup
│   └── task.py          # Task ORM model
├── api/
│   ├── routes.py        # CRUD endpoints for tasks
│   └── schemas.py       # Pydantic request/response schemas
└── utils/
    ├── auth.py          # API key authentication middleware
    └── logging_config.py# Structured JSON logging setup
```

## Running locally

```bash
pip install fastapi uvicorn sqlalchemy pydantic
uvicorn main:app --reload --port 8080
```

## Environment variables

| Variable       | Description                  | Default           |
|----------------|------------------------------|--------------------|
| `DATABASE_URL` | SQLAlchemy connection string | `sqlite:///tasks.db` |
| `API_KEY`      | Shared secret for auth       | *(required)*       |
| `LOG_LEVEL`    | Python log level             | `INFO`             |
