# migrate.py
"""
Универсальный скрипт для запуска всех миграций PostgreSQL
Запуск: python migrate.py
"""

import sys

def run_migration(script_name, description):
    print(f"\n{'='*50}")
    print(f"🚀 Запуск: {description}")
    print(f"{'='*50}")
    
    try:
        # Импортируем и запускаем модуль
        module = __import__(script_name.replace('.py', ''))
        
        # Если в модуле есть функция run(), вызываем её
        if hasattr(module, 'run'):
            module.run()
        else:
            # Иначе просто импортируем (код выполнится при импорте)
            print(f"✅ {description} выполнен")
            
    except Exception as e:
        print(f"❌ Ошибка в {description}: {e}")
        return False
    
    return True

if __name__ == "__main__":
    print("🎯 PostgreSQL Миграции для ФинУчет")
    print("="*50)
    
    # Список миграций в порядке выполнения
    migrations = [
        ("add_columns", "Добавление колонок в таблицу users"),
        ("add_investments_table", "Создание таблицы investments"),
    ]
    
    success = True
    for script, desc in migrations:
        if not run_migration(script, desc):
            success = False
            break
    
    print("\n" + "="*50)
    if success:
        print("🎉 Все миграции успешно выполнены!")
    else:
        print("❌ Миграции завершились с ошибкой")
        sys.exit(1)