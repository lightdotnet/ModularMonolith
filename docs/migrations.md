# dotnet-ef

### Update Tools
```bash
dotnet tool install --global dotnet-ef
```

### Add migrations
```
dotnet ef migrations add CreateIdentitySchema --context IdentityDbContext --output-dir Identity
```
```
dotnet ef migrations add CreateCatalogSchema --context CatalogContext --output-dir Catalog
```

### Update database
```
dotnet ef database update --context CatalogContext
```

