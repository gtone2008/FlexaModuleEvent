USE FlexaModuleEvent;

CREATE TABLE PDCOUNT2(
	ID			int auto_increment primary key,
	Line		varchar(10) NULL,
	ModuleID	int NULL,
	Times		varchar(50) NULL,
	ProgramName varchar(50) NULL,
	PickupCount int NULL,
	PartNo		varchar(30) NULL,
	FIDL		varchar(30) NULL
);

CREATE TABLE PDERROR(
	ID			int auto_increment primary key,
	Line		varchar(10) NULL,
	ModuleID	int NULL,
	Times		varchar(50) NULL,
	ProgramName varchar(50) NULL,
	ErrorCode	varchar(50) NULL,
	PosNo		varchar(30) NULL,
	PartNo		varchar(50) NULL,
	FIDL		varchar(50) NULL,
	NozzleSer   varchar(50) NULL
	
);


CREATE TABLE UNITINFO(
	ID			int auto_increment primary key,
	Line		varchar(10) NULL,
	ModuleID	int NULL,
	Times		varchar(50) NULL,
	UNITINFO	varchar(50) NULL,
	UNITType	varchar(50) NULL
);

CREATE TABLE BOARDCOUNT(
	ID			int auto_increment primary key,
	Line		varchar(10) NULL,
	ModuleID	int NULL,
	Times		varchar(50) NULL,
	ProgramName	varchar(50) NULL,
	BoardsSkipped	int NULL,
	BoardCount	int NULL
);

CREATE TABLE HeadType(
	ID			int auto_increment primary key,
	HeadType		varchar(50) NULL
);