import os
import requests
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import List, Optional
from recommender import Recommender

# -------------------------------
# Config
# -------------------------------
MOVIES_API = "https://localhost:7119/api/movies"  # your .NET API
REVIEWS_API = "https://localhost:7119/api/reviews/my-reviews"
PORT = int(os.environ.get("PORT", 8000))

app = FastAPI(title="Movie Recommender")

# Enable CORS for React frontend
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:3000"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

rec = Recommender()

# -------------------------------
# Schemas
# -------------------------------
class FilterRequest(BaseModel):
    genres: List[str] = []
    language: Optional[str] = None
    era: Optional[str] = None
    favoriteMovies: List[str] = []

# -------------------------------
# Startup: Load movies
# -------------------------------
@app.on_event("startup")
def startup_load():
    try:
        print("Fetching movies from .NET API...")
        movies = requests.get(MOVIES_API, verify=False).json()
        print(f"Fetched {len(movies)} movies")
        rec.fit(movies)
        print(f"Recommender initialized with {len(rec.movies)} movies")
    except Exception as e:
        print("Failed to fetch movies on startup:", e)

# -------------------------------
# Root
# -------------------------------
@app.get("/")
def read_root():
    return {"message": "Movie Recommender API running"}

# -------------------------------
# Recommend
# -------------------------------
@app.post("/recommend")
def recommend(filters: FilterRequest, user_email: Optional[str] = None):
    try:
        fav_movies = filters.favoriteMovies or []

        # Fetch user reviews if user_email is provided
        if user_email:
            try:
                reviews = requests.get(REVIEWS_API, verify=False).json()
                for r in reviews:
                    if r.get('rating', 0) >= 2 and 'movieTitle' in r:
                        fav_movies.append(r['movieTitle'])
            except Exception as e:
                print("Failed to fetch user reviews:", e)

        # Remove duplicates and ignore missing movies
        valid_favs = []
        for title in set(fav_movies):
            if rec._movie_index(title) is not None:
                valid_favs.append(title)

        filters_dict = filters.dict()
        filters_dict['favoriteMovies'] = valid_favs

        results = rec.recommend(filters_dict, top_n=20)
        return {"results": results}

    except Exception as e:
        print("Recommendation error:", e)
        raise HTTPException(status_code=500, detail=str(e))

# -------------------------------
# Retrain (reload movies)
# -------------------------------
@app.post("/retrain")
def retrain():
    try:
        movies = requests.get(MOVIES_API, verify=False).json()
        rec.fit(movies)
        return {"status": "ok", "count": len(movies)}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
