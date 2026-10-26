# ToDo ![Coverage](https://raw.githubusercontent.com/RasmusHS/ToDo/badges/coverage.svg)
A simple ToDo webapp. <br>
The main purpose of this project is to learn to work together as a group, hence the very simple task of making a ToDo app. 

## Tech stack
Language: C# <br>
Frameworks: .NET, Entity Framework, Blazor SSR, xUnit <br>
Database: Postgres <br>
Packages: Automapper, Fluentvalidation, OneOf, Moq, Testcontainers <br>
Infrastructure: Docker <br>

## Usage

1. Make sure docker and wsl is installed. (pc might need a restart before point 2)
    ```pwsh
    wsl.exe --install
    ```
    [Docker Desktop](https://www.docker.com/products/docker-desktop/)
2. Clone or download this project. And unzip if downloaded.
3. Make sure Docker is running.
4. Open terminal in the project folder or cd into it.
5. Run the following command:
   ```
   docker compose run -d
   ```
6. The API container sometimes shuts down because it tries to connect to the database too early and might need to be started up manually.
7. The API's address is http://localhost:5295/swagger and the blazor frontend is located at http://localhost:5038.

## Requirements

- [x] Single user application.
- [x] Be able to create ToDo lists.
- [x] Be able to view an overview of all lists.
- [x] Be able to view a specific ToDo list and its ToDo items.
- [x] Be able to update a ToDo list's title and description.
- [x] Be able to delete a ToDo list along with its ToDo items.
  - [x] Prompt user if they're sure when they want to delete a ToDo list.
- [x] Be able to create ToDo items to a ToDo list.
- [x] Be able to update a ToDo item's text, status, or whether it's done or not.
- [x] Be able to delete a ToDo item from a ToDo list.
  - [x] The user should not be prompted when deleting a ToDo item.
     
## Documentation

We made the choice to not have a Status entity as it is a one-to-many relationship with ToDoItemEntity that only covers 1 attribute. We felt it made more sense to have a status attribute in ToDoItemEntity and have the different options be hardcoded. <br>
With more time or better planning we would have made a user editable json document to store the different statuses. <br>

Tags were explored on how to implement and would result in up to 2 many-to-many relationships between a TagEntity and ToDoListEntity + ToDoItemEntity.
### Entity Relation Diagram

![V3](ERD_V3.png)

### Enhanced Entity Relation Diagram

![V1](EERD_V1.png)

## Architecture
We went back a forth between vertical slice with a seperate Persistence assembly or clean architecture. <br>
In the end we ended up with clean architecture where we left out the infrastructure layer due to the project's simple nature. <br>

Clean architecture adds a bit more complexity in our case, but some of the benefits seem worthwhile, like being able to pull logic that would've been in the api layer into a seperate application layer. <br>
Some Domain Driven Design aspects were considered like making the creating, querying, updating, and deletion of ToDoItemEntities go through ToDoListEntity, since a ToDo list, technically, has ownership over its associated ToDo items. This would affect things like being able to use automapper or not, as well as adding additional complexity when the main purpose of this project is to work together as a group. <br>

Other decisions of note includes docker and frontend. For frontend the decision was about making desktop app or a webapp, whereas we settled on a webapp. The decision to use docker was made early because it would allow frontend to be worked on more independently from the backend, as well as allowing for easier usage of program by 3rd parties. <br>

Below is a dependency diagram of our application. <br>
The application features domain, application, persistence, and presentation layers.

### Dependency Diagram

![V1](DependencyDiagram.png)
