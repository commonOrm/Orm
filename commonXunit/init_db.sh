docker-compose up -d

docker exec -it test_mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P a123456+ -d master -i /initdb/mssqldb.sql
