# db_config.py
import os
from sqlalchemy import create_engine

# Конфигурация PostgreSQL
DB_HOST = os.getenv("DB_HOST", "localhost")
DB_PORT = os.getenv("DB_PORT", "5432")
DB_NAME = os.getenv("DB_NAME", "finuchet")
DB_USER = os.getenv("DB_USER", "finuchet_user")
DB_PASSWORD = os.getenv("DB_PASSWORD", "finuchet_password")

# URL для подключения
DATABASE_URL = f"postgresql://{DB_USER}:{DB_PASSWORD}@{DB_HOST}:{DB_PORT}/{DB_NAME}"

def get_postgres_connection():
    """Возвращает сырое соединение с PostgreSQL для миграций"""
    import psycopg2
    return psycopg2.connect(
        host=DB_HOST,
        port=DB_PORT,
        database=DB_NAME,
        user=DB_USER,
        password=DB_PASSWORD
    )

def get_sqlalchemy_engine():
    """Возвращает SQLAlchemy engine для PostgreSQL"""
    return create_engine(DATABASE_URL)