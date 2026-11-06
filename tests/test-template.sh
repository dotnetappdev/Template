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
  if dotnet build "$OUT/$name/Acme.$name.sln" -nologo -v q -clp:ErrorsOnly \
     && { [ ! -d "$OUT/$name/tests" ] || dotnet test "$OUT/$name/Acme.$name.sln" --no-build -nologo -v q; }; then
    echo "--- $name: OK"
  else
    echo "!!! $name: build failed"; failed=1
  fi
}

run_case Default
run_case Everything --tests --serilog --api-key --health-checks --resilience --internal-auth Windows \
  --jquery-validation --datatables --select2 --toastr --sweetalert --chartjs --bootstrap-theme flatly --jquery-ui-theme redmond
run_case EntraBs4 --internal-auth EntraId --entra-tenant-id 11111111-2222-3333-4444-555555555555 --bootstrap 4.6.2 \
  --bootstrap-theme darkly --select2 --datatables --jquery-ui 1.12.1 --jquery 3.5.1 --fontawesome 5.15.4
run_case Minimal --tests false --swagger false --health-checks false --resilience false --seed-data false \
  --jquery-validation false --datatables false --toastr false --fontawesome none
run_case Net9Postgres -f net9.0 --database PostgreSQL --postgres-version 16 --jquery 4.0.0 --bootstrap 5.2.3 --fontawesome 7.3.1 --serilog
run_case Net10Sqlite -f net10.0 --database Sqlite --bootstrap 5.1.3 --fontawesome 6.4.2 --api-key --serilog --select2
run_case Net11Azure -f net11.0 --database SqlServer --sql-version AzureSql --db-auth SqlLogin \
  --db-server my-srv.database.windows.net --db-user app --db-password 'P@ss"w\rd' --serilog --internal-auth Windows
run_case InternalOnlyNoData --external false --services false --sql-repository false
run_case ExternalOnly --internal false --jquery-ui 1.13.3 --jquery-ui-theme smoothness
run_case ApiOnly --internal false --external false --api-key
run_case RepoNoServices --services false --sql-version SqlServer2014 --db-auth SqlLogin
run_case CustomConn --connection-string 'Server=.\SQLEXPRESS;Database=Custom;Trusted_Connection=True;TrustServerCertificate=True'

exit $failed
