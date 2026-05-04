# add_columns.py
import psycopg2
from db_config import get_postgres_connection

def add_columns():
    conn = None
    cursor = None
    try:
        conn = get_postgres_connection()
        cursor = conn.cursor()
        
        # Добавляем колонку email (IF NOT EXISTS для PostgreSQL)
        cursor.execute("""
            DO $$ 
            BEGIN 
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name='users' AND column_name='email') THEN
                    ALTER TABLE users ADD COLUMN email TEXT;
                END IF;
            END $$;
        """)
        print("✅ Колонка email проверена/добавлена")
        
        # Добавляем колонку security_question
        cursor.execute("""
            DO $$ 
            BEGIN 
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name='users' AND column_name='security_question') THEN
                    ALTER TABLE users ADD COLUMN security_question TEXT DEFAULT 'Какую кличку у вас было в детстве?';
                END IF;
            END $$;
        """)
        print("✅ Колонка security_question проверена/добавлена")
        
        # Добавляем колонку security_answer
        cursor.execute("""
            DO $$ 
            BEGIN 
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name='users' AND column_name='security_answer') THEN
                    ALTER TABLE users ADD COLUMN security_answer TEXT DEFAULT '';
                END IF;
            END $$;
        """)
        print("✅ Колонка security_answer проверена/добавлена")
        
        # Добавляем колонку is_admin
        cursor.execute("""
            DO $$ 
            BEGIN 
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name='users' AND column_name='is_admin') THEN
                    ALTER TABLE users ADD COLUMN is_admin BOOLEAN DEFAULT FALSE;
                END IF;
            END $$;
        """)
        print("✅ Колонка is_admin проверена/добавлена")
        
        # Добавляем колонку is_blocked
        cursor.execute("""
            DO $$ 
            BEGIN 
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name='users' AND column_name='is_blocked') THEN
                    ALTER TABLE users ADD COLUMN is_blocked BOOLEAN DEFAULT FALSE;
                END IF;
            END $$;
        """)
        print("✅ Колонка is_blocked проверена/добавлена")
        
        # Добавляем колонку created_at
        cursor.execute("""
            DO $$ 
            BEGIN 
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name='users' AND column_name='created_at') THEN
                    ALTER TABLE users ADD COLUMN created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
                END IF;
            END $$;
        """)
        print("✅ Колонка created_at проверена/добавлена")
        
        # Добавляем колонку block_until
        cursor.execute("""
            DO $$ 
            BEGIN 
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name='users' AND column_name='block_until') THEN
                    ALTER TABLE users ADD COLUMN block_until TIMESTAMP;
                END IF;
            END $$;
        """)
        print("✅ Колонка block_until проверена/добавлена")
        
        conn.commit()
        print("🎉 Все миграции успешно применены к PostgreSQL!")
        
    except Exception as e:
        print(f"❌ Ошибка при миграции: {e}")
        if conn:
            conn.rollback()
        raise
    finally:
        if cursor:
            cursor.close()
        if conn:
            conn.close()

if __name__ == "__main__":
    add_columns()