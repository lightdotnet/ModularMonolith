# Infrastructure

### PostgreSQL & pgAdmin
```
docker run -p 5432:5432 \
	--name postgres \
	-e POSTGRES_USER=admin \
	-e POSTGRES_PASSWORD=P@ssword \
	-e POSTGRES_DB=SeedDb \
	-d postgres 
```
```
docker run -p 5050:80 \
    -e "PGADMIN_DEFAULT_EMAIL=user@domain.com" \
    -e "PGADMIN_DEFAULT_PASSWORD=P@ssword" \
    -d dpage/pgadmin4
```

### Redis
```
docker run -d --name redis -p 6379:6379 redis:alpine redis-server --requirepass "123"
```

```
docker exec -it redis bash
```

---
_Last synced: 2026-09-30_
