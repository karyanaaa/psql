from fastapi import FastAPI, Depends, HTTPException, status
from fastapi.security import OAuth2PasswordBearer, OAuth2PasswordRequestForm
from sqlalchemy.orm import Session
from pydantic import BaseModel
from typing import List, Optional
from datetime import datetime, timedelta  # <-- Убедитесь что timedelta здесь
import models
import database
import auth_simple as auth
from fastapi.middleware.cors import CORSMiddleware

# Создание таблиц
models.Base.metadata.create_all(bind=database.engine)

# Создание администратора по умолчанию (если нет)
def create_default_admin():
    db = database.SessionLocal()
    try:
        admin = db.query(models.User).filter(models.User.username == "admin").first()
        if not admin:
            admin = models.User(
                username="admin",
                hashed_password=auth.get_password_hash("admin123"),
                email="admin@finuchet.com",
                is_admin=True,
                security_question="По умолчанию",
                security_answer=auth.get_password_hash("default")
            )
            db.add(admin)
            db.commit()
            print("✅ Администратор создан: admin / admin123")
    except Exception as e:
        print(f"Ошибка создания администратора: {e}")
    finally:
        db.close()

create_default_admin()

app = FastAPI(title="ФинУчет API")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

oauth2_scheme = OAuth2PasswordBearer(tokenUrl="token")

# --- Схемы ---
class UserCreate(BaseModel):
    username: str
    password: str
    security_question: Optional[str] = "Какую кличку у вас было в детстве?"
    security_answer: Optional[str] = None
    email: Optional[str] = ""

class TransactionCreate(BaseModel):
    amount: float
    description: str
    type: str
    category_id: int
    date: Optional[datetime] = None

class TransactionResponse(TransactionCreate):
    id: int
    class Config:
        from_attributes = True

class MessageCreate(BaseModel):
    subject: str
    message: str
    receiver_id: Optional[int] = None

class MessageResponseUpdate(BaseModel):
    response: str

class CategoryCreate(BaseModel):
    name: str
    type: str

class CategoryResponse(CategoryCreate):
    id: int
    user_id: int
    class Config:
        from_attributes = True

class InvestmentCreate(BaseModel):
    name: str
    type: str
    amount: float
    purchase_price: float
    current_price: float
    quantity: float
    purchase_date: Optional[datetime] = None
    currency: Optional[str] = "RUB"
    notes: Optional[str] = ""

class InvestmentResponse(InvestmentCreate):
    id: int
    user_id: int
    class Config:
        from_attributes = True

class ResetPasswordRequest(BaseModel):
    username: str
    security_answer: str
    new_password: str

# --- Зависимости ---
def get_current_user(token: str = Depends(oauth2_scheme), db: Session = Depends(database.get_db)):
    credentials_exception = HTTPException(status_code=401, detail="Invalid credentials")
    try:
        payload = auth.jwt.decode(token, auth.SECRET_KEY, algorithms=[auth.ALGORITHM])
        username: str = payload.get("sub")
        if username is None:
            raise credentials_exception
    except auth.JWTError:
        raise credentials_exception
    user = db.query(models.User).filter(models.User.username == username).first()
    if user is None:
        raise credentials_exception
    if user.is_blocked:
        # Проверяем, не истекла ли временная блокировка
        if user.block_until and user.block_until <= datetime.utcnow():
            user.is_blocked = False
            user.block_until = None
            db.commit()
        else:
            raise HTTPException(status_code=403, detail="User is blocked")
    return user

def get_current_admin(current_user: models.User = Depends(get_current_user)):
    if not current_user.is_admin:
        raise HTTPException(status_code=403, detail="Admin access required")
    return current_user

# --- Эндпоинты ---
@app.get("/security-question")
def get_security_question(username: str, db: Session = Depends(database.get_db)):
    user = db.query(models.User).filter(models.User.username == username).first()
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    return {"question": user.security_question}

@app.post("/register")
def register(user: UserCreate, db: Session = Depends(database.get_db)):
    db_user = db.query(models.User).filter(models.User.username == user.username).first()
    if db_user:
        raise HTTPException(status_code=400, detail="Username already registered")
    hashed_pw = auth.get_password_hash(user.password)
    hashed_answer = auth.get_password_hash(user.security_answer) if user.security_answer else ""
    
    db_user = models.User(
        username=user.username, 
        hashed_password=hashed_pw,
        security_question=user.security_question,
        security_answer=hashed_answer,
        email=user.email
    )
    db.add(db_user)
    db.commit()
    db.refresh(db_user)
    return {"msg": "User created"}

@app.post("/reset-password")
def reset_password(request: ResetPasswordRequest, db: Session = Depends(database.get_db)):
    user = db.query(models.User).filter(models.User.username == request.username).first()
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    if not auth.verify_password(request.security_answer, user.security_answer):
        raise HTTPException(status_code=401, detail="Incorrect security answer")
    user.hashed_password = auth.get_password_hash(request.new_password)
    db.commit()
    return {"msg": "Password reset successfully"}

@app.post("/token")
def login(form_data: OAuth2PasswordRequestForm = Depends(), db: Session = Depends(database.get_db)):
    user = db.query(models.User).filter(models.User.username == form_data.username).first()
    if not user or not auth.verify_password(form_data.password, user.hashed_password):
        raise HTTPException(status_code=401, detail="Incorrect username or password")
    
    if user.is_blocked:
        if user.block_until and user.block_until > datetime.utcnow():
            remaining = user.block_until - datetime.utcnow()
            hours = int(remaining.total_seconds() // 3600)
            minutes = int((remaining.total_seconds() % 3600) // 60)
            raise HTTPException(status_code=403, detail=f"Аккаунт заблокирован. Осталось: {hours}ч {minutes}мин")
        elif user.block_until and user.block_until <= datetime.utcnow():
            user.is_blocked = False
            user.block_until = None
            db.commit()
        else:
            raise HTTPException(status_code=403, detail="Аккаунт заблокирован навсегда")
    
    access_token = auth.create_access_token(data={"sub": user.username})
    return {"access_token": access_token, "token_type": "bearer", "is_admin": user.is_admin}

# --- Транзакции ---
@app.get("/transactions", response_model=List[TransactionResponse])
def get_transactions(db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    return db.query(models.Transaction).filter(models.Transaction.user_id == current_user.id).all()

@app.post("/transactions")
def create_transaction(tr: TransactionCreate, db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    data = tr.model_dump()
    data['user_id'] = current_user.id
    if 'date' not in data or data['date'] is None:
        data['date'] = datetime.utcnow()
    db_tr = models.Transaction(**data)
    db.add(db_tr)
    db.commit()
    db.refresh(db_tr)
    return db_tr

@app.put("/transactions/{transaction_id}")
def update_transaction(transaction_id: int, tr: TransactionCreate, db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    db_transaction = db.query(models.Transaction).filter(
        models.Transaction.id == transaction_id,
        models.Transaction.user_id == current_user.id
    ).first()
    if not db_transaction:
        raise HTTPException(status_code=404, detail="Transaction not found")
    db_transaction.amount = tr.amount
    db_transaction.description = tr.description
    db_transaction.type = tr.type
    db_transaction.category_id = tr.category_id
    db_transaction.date = tr.date or datetime.utcnow()
    db.commit()
    db.refresh(db_transaction)
    return db_transaction

@app.delete("/transactions/{tr_id}")
def delete_transaction(tr_id: int, db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    tr = db.query(models.Transaction).filter(models.Transaction.id == tr_id, models.Transaction.user_id == current_user.id).first()
    if not tr:
        raise HTTPException(status_code=404, detail="Not found")
    db.delete(tr)
    db.commit()
    return {"msg": "Deleted"}

# --- Категории ---
@app.get("/categories", response_model=List[CategoryResponse])
def get_categories(db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    return db.query(models.Category).filter(models.Category.user_id == current_user.id).all()

@app.post("/categories", response_model=CategoryResponse)
def create_category(category: CategoryCreate, db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    db_category = models.Category(
        name=category.name,
        type=category.type,
        user_id=current_user.id
    )
    db.add(db_category)
    db.commit()
    db.refresh(db_category)
    return db_category

@app.put("/categories/{category_id}", response_model=CategoryResponse)
def update_category(category_id: int, category: CategoryCreate, db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    db_category = db.query(models.Category).filter(
        models.Category.id == category_id,
        models.Category.user_id == current_user.id
    ).first()
    if not db_category:
        raise HTTPException(status_code=404, detail="Category not found")
    db_category.name = category.name
    db_category.type = category.type
    db.commit()
    db.refresh(db_category)
    return db_category

@app.delete("/categories/{category_id}")
def delete_category(category_id: int, db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    db_category = db.query(models.Category).filter(
        models.Category.id == category_id,
        models.Category.user_id == current_user.id
    ).first()
    if not db_category:
        raise HTTPException(status_code=404, detail="Category not found")
    transactions = db.query(models.Transaction).filter(models.Transaction.category_id == category_id).first()
    if transactions:
        raise HTTPException(status_code=400, detail="Cannot delete category with existing transactions")
    db.delete(db_category)
    db.commit()
    return {"msg": "Category deleted"}

# --- Инвестиции ---
@app.get("/investments", response_model=List[InvestmentResponse])
def get_investments(db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    return db.query(models.Investment).filter(models.Investment.user_id == current_user.id).all()

@app.post("/investments", response_model=InvestmentResponse)
def create_investment(inv: InvestmentCreate, db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    data = inv.model_dump()
    data['user_id'] = current_user.id
    if 'purchase_date' not in data or data['purchase_date'] is None:
        data['purchase_date'] = datetime.utcnow()
    db_inv = models.Investment(**data)
    db.add(db_inv)
    db.commit()
    db.refresh(db_inv)
    return db_inv

@app.put("/investments/{inv_id}", response_model=InvestmentResponse)
def update_investment(inv_id: int, inv: InvestmentCreate, db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    db_inv = db.query(models.Investment).filter(
        models.Investment.id == inv_id,
        models.Investment.user_id == current_user.id
    ).first()
    if not db_inv:
        raise HTTPException(status_code=404, detail="Investment not found")
    for key, value in inv.model_dump().items():
        setattr(db_inv, key, value)
    db.commit()
    db.refresh(db_inv)
    return db_inv

@app.delete("/investments/{inv_id}")
def delete_investment(inv_id: int, db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    db_inv = db.query(models.Investment).filter(
        models.Investment.id == inv_id,
        models.Investment.user_id == current_user.id
    ).first()
    if not db_inv:
        raise HTTPException(status_code=404, detail="Investment not found")
    db.delete(db_inv)
    db.commit()
    return {"msg": "Deleted"}

# --- Сообщения ---
@app.post("/messages")
def send_message(msg: MessageCreate, db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    receiver_id = msg.receiver_id
    if receiver_id is None:
        admin = db.query(models.User).filter(models.User.is_admin == True).first()
        if not admin:
            raise HTTPException(status_code=404, detail="No admin found")
        receiver_id = admin.id
    db_msg = models.UserMessage(
        sender_id=current_user.id,
        receiver_id=receiver_id,
        subject=msg.subject,
        message=msg.message
    )
    db.add(db_msg)
    db.commit()
    db.refresh(db_msg)
    return {"msg": "Message sent"}

@app.get("/messages")
def get_messages(db: Session = Depends(database.get_db), current_user: models.User = Depends(get_current_user)):
    messages = db.query(models.UserMessage).filter(
        (models.UserMessage.sender_id == current_user.id) | 
        (models.UserMessage.receiver_id == current_user.id)
    ).order_by(models.UserMessage.created_at.desc()).all()
    
    result = []
    for m in messages:
        sender = db.query(models.User).filter(models.User.id == m.sender_id).first()
        receiver = db.query(models.User).filter(models.User.id == m.receiver_id).first()
        result.append({
            "id": m.id,
            "sender_id": m.sender_id,
            "receiver_id": m.receiver_id,
            "subject": m.subject,
            "message": m.message,
            "response": m.response,
            "is_read": m.is_read,
            "created_at": m.created_at,
            "responded_at": m.responded_at,
            "sender_name": sender.username if sender else "Unknown",
            "receiver_name": receiver.username if receiver else "Unknown"
        })
    return result

@app.post("/messages/{message_id}/respond")
def respond_to_message(message_id: int, response: MessageResponseUpdate, db: Session = Depends(database.get_db), admin: models.User = Depends(get_current_admin)):
    message = db.query(models.UserMessage).filter(models.UserMessage.id == message_id).first()
    if not message:
        raise HTTPException(status_code=404, detail="Message not found")
    message.response = response.response
    message.responded_at = datetime.utcnow()
    db.commit()
    return {"msg": "Response sent"}

# --- Админские эндпоинты ---
@app.get("/admin/users")
def get_all_users(db: Session = Depends(database.get_db), admin: models.User = Depends(get_current_admin)):
    users = db.query(models.User).all()
    return [{
        "id": u.id,
        "username": u.username,
        "email": u.email,
        "is_admin": u.is_admin,
        "is_blocked": u.is_blocked,
        "block_until": u.block_until,
        "created_at": u.created_at
    } for u in users]

@app.post("/admin/users/block")
def block_user(request: dict, db: Session = Depends(database.get_db), admin: models.User = Depends(get_current_admin)):
    print(f"Received request: {request}")
    
    user_id = request.get("user_id")
    block = request.get("block")
    hours = request.get("hours", 0)
    
    print(f"user_id: {user_id}, block: {block}, hours: {hours}")
    
    if user_id is None:
        raise HTTPException(status_code=400, detail="user_id is required")
    
    user = db.query(models.User).filter(models.User.id == user_id).first()
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    if user.is_admin:
        raise HTTPException(status_code=403, detail="Cannot block admin")
    
    user.is_blocked = block
    if block and hours > 0:
        user.block_until = datetime.utcnow() + timedelta(hours=hours)
    else:
        user.block_until = None
    
    db.commit()
    return {"msg": f"User {'blocked' if block else 'unblocked'}"}

@app.delete("/admin/users/{user_id}")
def delete_user(user_id: int, db: Session = Depends(database.get_db), admin: models.User = Depends(get_current_admin)):
    user = db.query(models.User).filter(models.User.id == user_id).first()
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    if user.is_admin:
        raise HTTPException(status_code=403, detail="Cannot delete admin")
    
    db.query(models.Transaction).filter(models.Transaction.user_id == user_id).delete()
    db.query(models.Category).filter(models.Category.user_id == user_id).delete()
    db.query(models.Investment).filter(models.Investment.user_id == user_id).delete()
    db.query(models.UserMessage).filter(
        (models.UserMessage.sender_id == user_id) | 
        (models.UserMessage.receiver_id == user_id)
    ).delete()
    
    db.delete(user)
    db.commit()
    return {"msg": "User deleted"}

@app.get("/admin/messages")
def get_all_messages(db: Session = Depends(database.get_db), admin: models.User = Depends(get_current_admin)):
    messages = db.query(models.UserMessage).order_by(models.UserMessage.created_at.desc()).all()
    result = []
    for m in messages:
        sender = db.query(models.User).filter(models.User.id == m.sender_id).first()
        receiver = db.query(models.User).filter(models.User.id == m.receiver_id).first()
        result.append({
            "id": m.id,
            "sender_id": m.sender_id,
            "sender_name": sender.username if sender else "Unknown",
            "receiver_id": m.receiver_id,
            "receiver_name": receiver.username if receiver else "Unknown",
            "subject": m.subject,
            "message": m.message,
            "response": m.response,
            "is_read": m.is_read,
            "created_at": m.created_at,
            "responded_at": m.responded_at
        })
    return result

@app.get("/admin/stats")
def get_admin_stats(db: Session = Depends(database.get_db), admin: models.User = Depends(get_current_admin)):
    total_users = db.query(models.User).count()
    total_transactions = db.query(models.Transaction).count()
    total_investments = db.query(models.Investment).count()
    total_messages = db.query(models.UserMessage).count()
    unread_messages = db.query(models.UserMessage).filter(models.UserMessage.response == "").count()
    
    return {
        "total_users": total_users,
        "total_transactions": total_transactions,
        "total_investments": total_investments,
        "total_messages": total_messages,
        "unread_messages": unread_messages
    }