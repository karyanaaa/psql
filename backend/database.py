from sqlalchemy import create_engine
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker

# PostgreSQL подключение
# Формат: postgresql://username:password@host:port/database
SQLALCHEMY_DATABASE_URL = "postgresql://finuchet_user:finuchet_password@localhost:5432/finuchet"

# Создаём движок БД для PostgreSQL
engine = create_engine(
    SQLALCHEMY_DATABASE_URL,
    pool_size=5,
    max_overflow=10
)

# Создаём сессию
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

# Создаём Base
Base = declarative_base()

def get_db():
    """ыы
    Зависимость FastAPI для получения сессии БД.
    Автоматически закрывает соединение после запроса.
    """
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()