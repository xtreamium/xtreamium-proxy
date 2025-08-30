DROP TABLE IF EXISTS xt_Recordings;

create table xt_Recordings
(
  Id         INTEGER
    constraint xt_Recordings_pk
      primary key autoincrement,
  JobId      VARCHAR(50)  not null,
  Url        TEXT         not null,
  Title      varchar(255) not null,
  StartTime  TEXT         not null,
  Duration   INTEGER      not null,
  IsRecorded BOOLEAN      NOT NULL DEFAULT 0,
  FilePath   TEXT
);
