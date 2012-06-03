create table ServiceTable (
  ServiceTableID int not null,
  DescServiceTable varchar(200) not null,
  Value float not null,
  CreationDate datetime not null,
  StringField1 varchar(200),
  StringField2 varchar(200),
  constraint PK_ServiceTable primary key (ServiceTableID) 
)

create table ClientTable (
  ClientTableID int not null,
  DescClientTable varchar(200) not null,
  Value float not null,
  CreationDate datetime not null,
  StringField1 varchar(200),
  StringField2 varchar(200),
  constraint PK_ClientTable primary key (ClientTableID) 

)