.PHONY: build test lint

# Builds the source update package; the Windows game still requires Unity.
build:
	node scripts/check-docs.js
	python3 -B scripts/package-stage.py

test:
	npm test --prefix TikTokBridge
	python3 -B scripts/package-stage.py --check

lint:
	git diff --check
	node --check scripts/check-docs.js
	python3 -B -c "import ast, pathlib; ast.parse(pathlib.Path('scripts/package-stage.py').read_text()); print('Lint: OK')"
