CREATE USER cashflow WITH PASSWORD 'cashflow123';

CREATE DATABASE launch_db OWNER cashflow;
CREATE DATABASE daily_balance_db OWNER cashflow;

GRANT ALL PRIVILEGES ON DATABASE launch_db TO cashflow;
GRANT ALL PRIVILEGES ON DATABASE daily_balance_db TO cashflow;
