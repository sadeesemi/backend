# recommender.py
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity
from dateutil import parser
import numpy as np
import re

def _get_year(release_date):
    if not release_date:
        return None
    try:
        if isinstance(release_date, (int, float)):
            return int(release_date)
        return parser.parse(str(release_date)).year
    except Exception:
        m = re.search(r"(\d{4})", str(release_date))
        return int(m.group(1)) if m else None

def _in_era(year, era_str):
    if year is None or not era_str:
        return False
    if "Before 1980" in era_str:
        return year < 1980
    if "1980" in era_str and "2000" in era_str:
        return 1980 <= year <= 2000
    if "2000" in era_str:
        return year >= 2000
    return True

class Recommender:
    def __init__(self):
        self.vectorizer = TfidfVectorizer(stop_words='english', ngram_range=(1,2), max_features=20000)
        self.movies = []
        self.matrix = None
        self.title_to_index = {}

    def _build_corpus(self, movies):
        corpus = []
        for m in movies:
            parts = [
                m.get('title','') or '',
                m.get('description','') or '',
                ' '.join(m.get('genres', []) or []),
                m.get('director','') or ''
            ]
            cast = m.get('castMembers') or m.get('cast') or m.get('actors') or ''
            if isinstance(cast, list):
                parts.append(' '.join(cast))
            else:
                parts.append(str(cast))
            parts.append(m.get('language','') or '')
            year = _get_year(m.get('releaseDate') or m.get('year'))
            if year:
                parts.append(str(year))
            corpus.append(' '.join([p for p in parts if p]))
        return corpus

    def fit(self, movies):
        for m in movies:
            m['title'] = (m.get('title') or '').strip()
            m['movieID'] = m.get('movieID') or m.get('MovieID') or m.get('id')
            if 'genres' not in m:
                m['genres'] = m.get('Genres') or []
            if 'year' not in m or m.get('year') is None:
                yr = _get_year(m.get('releaseDate'))
                if yr:
                    m['year'] = yr
        self.movies = movies
        corpus = self._build_corpus(movies)
        self.matrix = self.vectorizer.fit_transform(corpus)
        self.title_to_index = { (m.get('title') or '').strip().lower(): i for i,m in enumerate(movies) }

    def _text_to_vec(self, text):
        return self.vectorizer.transform([text]).toarray()  # ensure ndarray

    def _movie_index(self, title):
        if not title:
            return None
        return self.title_to_index.get(title.strip().lower())

    def recommend(self, filters: dict, top_n: int = 20):
        if self.matrix is None:
            raise RuntimeError("Model is not fitted yet")

        selected_genres = [g.lower() for g in (filters.get('genres') or [])]
        lang = (filters.get('language') or '').strip().lower()
        era = filters.get('era') or ''

        candidates = []
        for idx, m in enumerate(self.movies):
            if lang and (m.get('language') or '').strip().lower() != lang:
                continue
            if era:
                year = _get_year(m.get('releaseDate') or m.get('year'))
                if not _in_era(year, era):
                    continue
            if selected_genres:
                movie_genres = [g.lower() for g in (m.get('genres') or [])]
                if not set(selected_genres).intersection(movie_genres):
                    continue
            candidates.append(idx)

        if not candidates:
            candidates = list(range(len(self.movies)))

        favs = filters.get('favoriteMovies') or []
        fav_indexes, fav_weights = [], []
        for title in favs:
            idx = self._movie_index(title)
            if idx is not None:
                fav_indexes.append(idx)
                fav_weights.append(1.0)

        if fav_indexes:
            fav_matrix = self.matrix[fav_indexes]
            weights = np.array(fav_weights).reshape(-1,1)
            weighted = fav_matrix.multiply(weights)
            user_vec = weighted.sum(axis=0)
            user_vec = np.asarray(user_vec).ravel().reshape(1, -1)  # fix np.matrix issue
        else:
            parts = filters.get('genres') or []
            if filters.get('language'):
                parts.append(filters.get('language'))
            if filters.get('era'):
                parts.append(filters.get('era'))
            user_text = ' '.join(parts).strip() or 'movie'
            user_vec = self._text_to_vec(user_text)

        cand_matrix = self.matrix[candidates]
        sims = cosine_similarity(cand_matrix, user_vec).flatten()
        pairs = sorted(zip(candidates, sims), key=lambda x: x[1], reverse=True)

        results, included_titles = [], set()

        for title in [t.strip() for t in favs]:
            if not title:
                continue
            idx = self._movie_index(title)
            if idx is None:
                continue
            m = dict(self.movies[idx])
            if m.get('title') in included_titles:
                continue
            included_titles.add(m.get('title'))
            results.append({
                "movieID": m.get('movieID'),
                "title": m.get('title'),
                "image": m.get('image'),
                "language": m.get('language'),
                "year": _get_year(m.get('releaseDate')) or m.get('year'),
                "genres": m.get('genres'),
                "score": 1.0
            })

        for idx, score in pairs:
            if len([r for r in results if r.get('score') != 1.0]) >= top_n:
                break
            m = dict(self.movies[idx])
            title = m.get('title')
            if title in included_titles:
                continue
            included_titles.add(title)
            results.append({
                "movieID": m.get('movieID'),
                "title": title,
                "image": m.get('image'),
                "language": m.get('language'),
                "year": _get_year(m.get('releaseDate')) or m.get('year'),
                "genres": m.get('genres'),
                "score": float(score)
            })

        return results
