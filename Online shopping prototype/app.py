from flask import Flask, render_template, request, redirect, url_for, flash, session
from flask_login import LoginManager, UserMixin, login_user, login_required, logout_user, current_user
from werkzeug.security import generate_password_hash, check_password_hash
from flask_pymongo import PyMongo
from dotenv import load_dotenv
from flask_session import Session  # Flask session management
import os
from bson import ObjectId  # Fixes ObjectId issue for MongoDB
import base64
# Load environment variables from .env
load_dotenv()

app = Flask(__name__)

# Secret key for session security (Ensure it's properly loaded from .env)
app.config['SECRET_KEY'] = os.getenv('SECRET_KEY', 'fallback-secret-key')

# Flask session config (for session persistence)
app.config['SESSION_TYPE'] = 'filesystem'
app.config['SESSION_PERMANENT'] = False
Session(app)

# MongoDB Configuration
atlas_uri = os.getenv('MONGO_URI') # Check if MONGO_URI is set in .env
if atlas_uri:
    # If MONGO_ATLAS_URI is set, use it
    app.config['MONGO_URI'] = atlas_uri
    print("Using Atlas URI:", atlas_uri)
else: #try to use local mongodb
    app.config['MONGO_URI'] = f"mongodb://{os.getenv('MONGO_HOST', 'localhost')}:{os.getenv('MONGO_PORT', '27017')}/{os.getenv('MONGO_DB', 'project2')}"

# Initialize PyMongo
mongo = PyMongo(app)

# Initialize Login Manager
login_manager = LoginManager()
login_manager.init_app(app)
login_manager.login_view = 'login'

ALLOWED_EXTENSIONS = {'png', 'jpg', 'jpeg', 'gif'}
app.config['UPLOAD_FOLDER'] = os.path.join('static', 'uploads')
if not os.path.exists(app.config['UPLOAD_FOLDER']):
    os.makedirs(app.config['UPLOAD_FOLDER'])

def allowed_file(filename):
    return '.' in filename and filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS

# User Model
class User(UserMixin):
    def __init__(self, username, email, password_hash, _id=None):
        self.id = str(_id) if _id else None  # Ensure id is stored as a string
        self.username = username
        self.email = email
        self.password_hash = password_hash

    def set_password(self, password):
        self.password_hash = generate_password_hash(password)

    def check_password(self, password):
        return check_password_hash(self.password_hash, password)

    def get_id(self):
        return str(self.id)  # Ensure Flask-Login can retrieve the user ID correctly

# Load user for Flask-Login
@login_manager.user_loader
def load_user(user_id):
    try:
        user_data = mongo.db.users.find_one({"_id": ObjectId(user_id)})  # Convert to ObjectId
        if not user_data:
            return None
        return User(user_data['username'], user_data['email'], user_data['password_hash'], user_data['_id'])
    except:
        return None  # Handle invalid ObjectId format

# **Home Route -> Now Redirects to Dashboard First**
@app.route('/')
def home():
    return redirect(url_for('dashboard'))  # Always direct to the dashboard

# Register Route
@app.route('/register', methods=['GET', 'POST'])
def register():
    if request.method == 'POST':
        username = request.form['username']
        email = request.form['email']
        password = request.form['password']

        # Check if username already exists
        if mongo.db.users.find_one({"username": username}):
            flash('Username already exists.', 'danger')
            return redirect(url_for('register'))

        # Create new user
        user = User(username, email, None)
        user.set_password(password)
        
        # Insert user and retrieve the _id
        inserted_user = mongo.db.users.insert_one({
            "username": user.username,
            "email": user.email,
            "password_hash": user.password_hash
        })

        user.id = str(inserted_user.inserted_id)  # Assign the correct _id
        login_user(user)
        return redirect(url_for('dashboard'))

    return render_template('register.html')

# Login Route
@app.route('/login', methods=['GET', 'POST'])
def login():
    if request.method == 'POST':
        username = request.form['login_input']
        password = request.form['password']
        user_data = mongo.db.users.find_one({"$or": [{"username": username}, {"email": username}]})

        if user_data:
            user = User(user_data['username'], user_data['email'], user_data['password_hash'], user_data['_id'])
            if user.check_password(password):
                login_user(user)
                return redirect(url_for('dashboard'))

        flash('Invalid username or password.', 'danger')

    return render_template('login.html')

# Dashboard Route (Protected)
@app.route('/dashboard')
def dashboard():
    # Fetch all products
    products = list(mongo.db.products.find())

    # Get all unique tags for the filter dropdown
    current_tags = mongo.db.products.distinct("tag")

    # Fetch comments for each product and attach them
    for product in products:
        product["_id"] = str(product["_id"])  # Convert ObjectId to string for Jinja compatibility
        product["comments"] = list(mongo.db.comments.find({"product_id": product["_id"]}))

    return render_template('dashboard.html', current_user=current_user, products=products,tags=current_tags)

    # return render_template('dashboard.html', current_user=None, products=[])


# Logout Route
@app.route('/logout')
@login_required
def logout():
    logout_user()
    flash('You have been logged out.', 'info')
    return redirect(url_for('dashboard')) # Since unauthorized users can view products now, redirect to dashboard

#profile route
@app.route('/profile', methods=['GET', 'POST'])
@login_required
def profile():
    
    if "products" not in mongo.db.list_collection_names():
        user_products = []  # No collection exists yet
    else:
        user_products = list(mongo.db.products.find({"username": current_user.username}))

    if request.method == 'POST':
        # product info
        product_name = request.form.get('product_name')
        product_image = request.files.get('product_image')
        description = request.form.get('description')
        price = request.form.get('price')
        pickup_location = request.form.get('pickup_location')
        inventory = request.form.get('inventory')
        tag = request.form.get('tag')

        image_data = None
        image_type = None
        if product_image and allowed_file(product_image.filename):
            # Get the MIME, such as "image/png"
            image_type = product_image.mimetype  
            raw_bytes = product_image.read()  # Read the image file
            image_data = base64.b64encode(raw_bytes).decode('utf-8') 

        # Insert the product into the database
        product = {
            "username": current_user.username,   # The current user's username
            "product_name": product_name,
            "description": description,
            "price": price,
            "pickup_location": pickup_location,
            "inventory": inventory,
            "tag": tag,
            "image_data": image_data,   # Base64 encoded image data
            "image_type": image_type    # MIME
        }
        mongo.db.products.insert_one(product)
        flash("Product posted successfully!", "success")
        return redirect(url_for('profile'))
    return render_template('profile.html', user=current_user, user_products = user_products)


@app.route("/edit_product/<product_id>", methods=["GET", "POST"])
@login_required
def edit_product(product_id):
    # Find the product by ID and ensure it belongs to the logged-in user
    product = mongo.db.products.find_one({"_id": ObjectId(product_id), "username": current_user.username})

    if not product:
        flash("Unauthorized or product not found.", "danger")
        return redirect(url_for("profile"))

    if request.method == "POST":
        updated_data = {
            "product_name": request.form["product_name"],
            "description": request.form["description"],
            "price": float(request.form["price"]),
            "inventory": int(request.form["inventory"]),
            "tag": request.form["tag"]
        }
        mongo.db.products.update_one({"_id": ObjectId(product_id)}, {"$set": updated_data})
        flash("Product updated successfully!", "success")
        return redirect(url_for("profile"))

    return render_template("edit_product.html", product=product)

@app.route("/delete_product/<product_id>")
@login_required
def delete_product(product_id):
    # Find the product by ID and ensure it belongs to the logged-in user
    product = mongo.db.products.find_one({"_id": ObjectId(product_id), "username": current_user.username})

    if product:
        mongo.db.products.delete_one({"_id": ObjectId(product_id)})
        flash("Product deleted successfully!", "success")
    else:
        flash("Unauthorized or product not found.", "danger")

    return redirect(url_for("profile"))

@app.route("/product/<product_id>", methods=["GET", "POST"])
@login_required  # Restrict access to logged-in users
def product_detail(product_id):
    product = mongo.db.products.find_one({"_id": ObjectId(product_id)})

    if not product:
        flash("Product not found.", "danger")
        return redirect(url_for("dashboard"))

    # Fetch comments for this product
    comments = list(mongo.db.comments.find({"product_id": product_id}))

    if request.method == "POST":
        comment_text = request.form.get("comment")
        if comment_text:
            comment = {
                "username": current_user.username,
                "comment": comment_text,
                "product_id": product_id
            }
            mongo.db.comments.insert_one(comment)
            flash("Comment added successfully!", "success")
            return redirect(url_for("product_detail", product_id=product_id))

    return render_template("product_detail.html", product=product, comments=comments)

@app.route("/search")
def search_product():
    search_query = request.args.get('q', '')
    selected_tag = request.args.get('tag', '')
    
    # Build the search query for MongoDB
    search_filter = {}
    
    if search_query:
        # Search in both product name and description (case-insensitive)
        search_filter['$or'] = [
            {"product_name": {"$regex": search_query, "$options": "i"}},
            {"description": {"$regex": search_query, "$options": "i"}}
        ]
    
    if selected_tag:
        # Filter by the selected tag
        search_filter["tag"] = selected_tag
    
    # If no filters are applied, show all products
    if search_filter:
        products = list(mongo.db.products.find(search_filter))
    else:
        products = list(mongo.db.products.find())
    
    # Get all unique tags for the filter dropdown
    all_tags = mongo.db.products.distinct("tag")
    
    # Convert ObjectId to string for each product
    for product in products:
        product["_id"] = str(product["_id"])
        product["comments"] = list(mongo.db.comments.find({"product_id": product["_id"]}))
    
    return render_template('dashboard.html', 
                          current_user=current_user, 
                          products=products, 
                          search_query=search_query,
                          selected_tag=selected_tag,
                          all_tags=all_tags)
    

# Run the Flask App
if __name__ == '__main__':
    app.run(debug=True)