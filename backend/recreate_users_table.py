# recreate_users_table.py
import psycopg2
from db_config import get_postgres_connection
from auth_simple import get_password_hash

def recreate_users_table():
    conn = None
    cursor = None
    try:
        conn = get_postgres_connection()
        cursor = conn.cursor()
        
        # Удаляем старую таблицу (ВНИМАНИЕ: все данные потеряются!)
        cursor.execute("DROP TABLE IF EXISTS users CASCADE")
        print("✅ Старая таблица users удалена")
        
        # Создаём новую таблицу с правильной структурой
        cursor.execute("""
            CREATE TABLE users (
                id SERIAL PRIMARY KEY,
                username TEXT UNIQUE NOT NULL,
                hashed_password TEXT NOT NULL,
                email TEXT,
                security_question TEXT NOT NULL DEFAULT 'Какую кличку у вас было в детстве?',
                security_answer TEXT,
                is_admin BOOLEAN DEFAULT FALSE,
                is_blocked BOOLEAN DEFAULT FALSE,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                block_until TIMESTAMP
            )
        """)
        print("✅ Новая таблица users создана")
        
        # Создаём индексы
        cursor.execute("CREATE INDEX idx_users_username ON users(username)")
        cursor.execute("CREATE INDEX idx_users_is_admin ON users(is_admin)")
        cursor.execute("CREATE INDEX idx_users_is_blocked ON users(is_blocked)")
        print("✅ Индексы созданы")
        
        # Создаём администратора по умолчанию
        admin_password = get_password_hash("admin123")
        cursor.execute("""
            INSERT INTO users (username, hashed_password, email, security_question, security_answer, is_admin)
            VALUES (%s, %s, %s, %s, %s, %s)
        """, ("admin", admin_password, "admin@finuchet.com", "По умолчанию", None, True))
        print("✅ Администратор создан: admin / admin123")
        
        conn.commit()
        print("🎉 Таблица users успешно пересоздана в PostgreSQL!")
        
    except Exception as e:
        print(f"❌ Ошибка: {e}")
        if conn:
            conn.rollback()
        raise
    finally:
        if cursor:
            cursor.close()
        if conn:
            conn.close()

if __name__ == "__main__":
    print("⚠️ ВНИМАНИЕ! Это удалит ВСЕХ пользователей и их данные!")
    confirm = input("Вы уверены? (yes/no): ")
    if confirm.lower() == "yes":
        recreate_users_table()
    else:
        print("Операция отменена")