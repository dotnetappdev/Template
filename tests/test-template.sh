#!/usr/bin/env bash
# Instantiates the template with a matrix of options and builds every generated solution.
# Usage: tests/test-template.sh [output-dir]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="${1:-$ROOT/artifacts/template-tests}"
rm -rf "$OUT" && mkdir -p "$OUT"

dotnet new uninstall "$ROOT/templates/RazorSolution" >/dev/null 2>&1 || true
dotnet new install "$ROOT/templates/RazorSolution" >/dev/null

failed=0

run_case() {
  local name="$1"; shift
  echo "=== $name: $*"
  if ! dotnet new razorsln -n "Acme.$name" -o "$OUT/$name" --no-restore "$@" >/dev/null; then
    echo "!!! $name: dotnet new failed"; failed=1; return
  fi
  if dotnet build "$OUT/$name/Acme.$name.sln" -nologo -v q -clp:ErrorsOnly; then
    echo "--- $name: OK"
  else
    echo "!!! $name: build failed"; failed=1
  fi
}

run_case Default
run_case Net9Postgres -f net9.0 --database PostgreSQL --postgres-version 16 --jquery 4.0.0 --bootstrap 5.2.3 --fontawesome 7.3.1
run_case Net10Sqlite -f net10.0 --database Sqlite --bootstrap 4.6.2 --fontawesome 5.15.4
run_case Net11Azure -f net11.0 --database SqlServer --sql-version AzureSql --db-auth SqlLogin \
  --db-server my-srv.database.windows.net --db-user app --db-password 'P@ss"w\rd'
run_case InternalOnlyNoData --external false --services false --sql-repository false
run_case ExternalOnly --internal false --jquery-ui 1.13.3
run_case ApiOnly --internal false --external false
run_case RepoNoServices --services false --sql-version SqlServer2016 --db-auth SqlLogin
run_case CustomConn --connection-string 'Server=.\SQLEXPRESS;Database=Custom;Trusted_Connection=True;TrustServerCertificate=True'

exit $failed
