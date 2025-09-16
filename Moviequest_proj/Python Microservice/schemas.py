# schemas.py
from pydantic import BaseModel
from typing import List, Optional

class FilterRequest(BaseModel):
    genres: List[str] = []
    language: Optional[str] = None
    era: Optional[str] = None
    favoriteMovies: List[str] = []
