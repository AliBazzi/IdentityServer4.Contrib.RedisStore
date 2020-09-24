up:
	make down
	docker-compose up -d

down:
	docker-compose down

test:
	make up
	dotnet test
	make down