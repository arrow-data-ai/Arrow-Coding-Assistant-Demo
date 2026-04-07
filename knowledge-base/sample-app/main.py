"""FastAPI application entry point for the Task Manager API."""

import os

from fastapi import FastAPI
from contextlib import asynccontextmanager

from models.database import engine, Base
from api.routes import router as task_router
from utils.logging_config import setup_logging


@asynccontextmanager
async def lifespan(app: FastAPI):
    setup_logging(os.environ.get("LOG_LEVEL", "INFO"))
    Base.metadata.create_all(bind=engine)
    yield


app = FastAPI(
    title="Task Manager API",
    version="1.0.0",
    lifespan=lifespan,
)

app.include_router(task_router, prefix="/api/v1/tasks", tags=["tasks"])


@app.get("/health")
async def health_check():
    return {"status": "ok"}
