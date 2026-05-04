# add_investments_table.py
import psycopg2
from db_config import get_postgres_connection

def create_investments_table():
    conn = None
    cursor = None
    try:
        conn = get_postgres_connection()
        cursor = conn.cursor()
        
        # Проверяем существование таблицы
        cursor.execute("""
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_name = 'investments'
            );
        """)
        
        table_exists = cursor.fetchone()[0]
        
        if not table_exists:
            # Создаём таблицу investments для PostgreSQL
            cursor.execute("""
                CREATE TABLE investments (
                    id SERIAL PRIMARY KEY,
                    name TEXT NOT NULL,
                    type TEXT NOT NULL,
                    amount REAL DEFAULT 0,
                    purchase_price REAL DEFAULT 0,
                    current_price REAL DEFAULT 0,
                    quantity REAL DEFAULT 0,
                    purchase_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    currency TEXT DEFAULT 'RUB',
                    notes TEXT DEFAULT '',
                    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE
                )
            """)
            print("✅ Таблица investments создана")
            
            # Создаём индекс для ускорения запросов
            cursor.execute("""
                CREATE INDEX idx_investments_user_id ON investments(user_id)
            """)
            print("✅ Индекс для investments создан")
        else:
            print("✅ Таблица investments уже существует")
        
        conn.commit()
        print("🎉 Таблица investments успешно настроена в PostgreSQL!")
        
    except Exception as e:
        print(f"❌ Ошибка при создании таблицы: {e}")
        if conn:
            conn.rollback()
        raise
    finally:
        if cursor:
            cursor.close()
        if conn:
            conn.close()

if __name__ == "__main__":
    create_investments_table()