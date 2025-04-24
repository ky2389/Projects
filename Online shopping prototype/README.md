# Web Application Exercise

A little exercise to build a web application following an agile development process. See the [instructions](instructions.md) for more detail.

## Team Name: La Verdadera Destreza

## Team members

- [Peng Jiang](https://github.com/PengJiang-Victor)
- [Jialiang Tang](https://github.com/JialiangTang1)
- [Yuquan Hu](https://github.com/N-A-E-S)
- [Henry Yu](https://github.com/ky2389)

## Product vision statement

**A prototype MongoDB-based online trading platform exclusive for NYU students to buy and sell second-hand items.**

## User stories

[click to jump to the user stories issues](https://github.com/software-students-spring2025/2-web-app-la-verdadera-destreza/issues)

## Steps necessary to run the software

### Prerequisites
- Python 3.11 or higher
- Docker Desktop installed

### Clone the repository
Clone the repository by this command line:

```sh
git clone https://github.com/software-students-spring2025/2-web-app-segfaultsquad.git <your_dirname>
```

### Download all requirements
1. create a virtual environment in the project file
```sh
python -m venv venv
```
2. activate the virtual environment
```sh
source venv/Scripts/activate
```
3. install requirements in the virtual environments
```sh
pip install -r requirements.txt
```

### Set up the .env file
1. create a .env file which include some configuration which helps you to connect with mongoDB data base.

```ini
# Secret Key
    SECRET_KEY=yourSecretKey

# If using local mongodb MongoDB (assume no authentication set up for your local mongodb)
MONGO_DB= your database name(on local now)
MONGO_HOST=your localhost
MONGO_PORT= your port for MongoDB (default: 27017)
#If using Atlas
MONGO_URI=mongodb+srv://your_username:your_password@cluster0.m3eme.mongodb.net/project2?retryWrites=true&w=majority&appName=Cluster0
```

### Set up Docker
1. Build and start the containers using Docker Compose:

```sh
docker run -d -p 27017:27017 --name=mongo-example mongo:latest
```
2. Connect to database
```sh
docker exec -it mongo-example mongosh
```

### Run the application
After running this command, MongoDB will be running in a Docker container, accessible on localhost:27017, and it will be running on http://127.0.0.1:5000



## Task boards

[Click to jump to status board.](https://github.com/orgs/software-students-spring2025/projects/91/views/2)
