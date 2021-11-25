# Orm
Orm

### 备份数据库 postgrepsql
```
pg_dump -h 127.0.0.1 -p 5432 -U postgres -c -C -f xiroumedicalcare.sql  xiroumedicalcare
psql -h 127.0.0.1 -p 5432 -U postgres -f xiroumedicalcare.sql
```
