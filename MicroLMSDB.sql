-- Script PostgreSQL MicroLMS - Schema unique public
-- Toutes les tables de l'application sont dans le schema public.

-- =========================================================
-- Tables de l'application (schema public)
-- =========================================================
-- Note: Le schema lms (legacy) a ete supprime. Toutes les tables sont dans public.

CREATE TABLE IF NOT EXISTS public."Roles" (
    "Id" SERIAL PRIMARY KEY,
    "Name" varchar(100) NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Roles_Name" ON public."Roles" ("Name");

CREATE TABLE IF NOT EXISTS public."Users" (
    "Id" SERIAL PRIMARY KEY,
    "UserName" varchar(100) NOT NULL,
    "Email" varchar(200) NOT NULL,
    "PasswordHash" varchar(500) NOT NULL,
    "RoleId" integer NOT NULL,
    CONSTRAINT "FK_Users_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES public."Roles" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_UserName" ON public."Users" ("UserName");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON public."Users" ("Email");

CREATE TABLE IF NOT EXISTS public."Quizzes" (
    "Id" SERIAL PRIMARY KEY,
    "Title" varchar(200) NOT NULL,
    "PassingScore" integer NOT NULL
);

CREATE TABLE IF NOT EXISTS public."Modules" (
    "Id" SERIAL PRIMARY KEY,
    "Title" varchar(200) NOT NULL,
    "Description" varchar(2000) NOT NULL,
    "QuizId" integer NULL,
    CONSTRAINT "FK_Modules_Quizzes_QuizId" FOREIGN KEY ("QuizId") REFERENCES public."Quizzes" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_Modules_QuizId" ON public."Modules" ("QuizId");

CREATE TABLE IF NOT EXISTS public."Lessons" (
    "Id" SERIAL PRIMARY KEY,
    "Title" varchar(200) NOT NULL,
    "TextContent" varchar(4000) NOT NULL,
    "VideoUrl" varchar(500) NULL,
    "PdfPath" varchar(500) NULL,
    "Order" integer NOT NULL,
    "ModuleId" integer NOT NULL,
    CONSTRAINT "FK_Lessons_Modules_ModuleId" FOREIGN KEY ("ModuleId") REFERENCES public."Modules" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Lessons_ModuleId" ON public."Lessons" ("ModuleId");

CREATE TABLE IF NOT EXISTS public."Questions" (
    "Id" SERIAL PRIMARY KEY,
    "Statement" varchar(1000) NOT NULL,
    "Type" integer NOT NULL,
    "QuizId" integer NOT NULL,
    CONSTRAINT "FK_Questions_Quizzes_QuizId" FOREIGN KEY ("QuizId") REFERENCES public."Quizzes" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Questions_QuizId" ON public."Questions" ("QuizId");

CREATE TABLE IF NOT EXISTS public."Options" (
    "Id" SERIAL PRIMARY KEY,
    "QuestionId" integer NOT NULL,
    "Text" text NOT NULL,
    "IsCorrect" boolean NOT NULL,
    CONSTRAINT "FK_Options_Questions_QuestionId" FOREIGN KEY ("QuestionId") REFERENCES public."Questions" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Options_QuestionId" ON public."Options" ("QuestionId");

CREATE TABLE IF NOT EXISTS public."Enrollments" (
    "Id" SERIAL PRIMARY KEY,
    "EnrollmentDate" timestamp NOT NULL,
    "StudentId" varchar(450) NOT NULL,
    "ModuleId" integer NOT NULL,
    "IsCompleted" boolean NOT NULL,
    CONSTRAINT "FK_Enrollments_Modules_ModuleId" FOREIGN KEY ("ModuleId") REFERENCES public."Modules" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Enrollments_ModuleId" ON public."Enrollments" ("ModuleId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Enrollments_StudentId_ModuleId" ON public."Enrollments" ("StudentId", "ModuleId");

CREATE TABLE IF NOT EXISTS public."LessonProgressions" (
    "Id" SERIAL PRIMARY KEY,
    "StudentId" varchar(450) NOT NULL,
    "LessonId" integer NOT NULL,
    "IsRead" boolean NOT NULL,
    "ReadDate" timestamp NOT NULL,
    CONSTRAINT "FK_LessonProgressions_Lessons_LessonId" FOREIGN KEY ("LessonId") REFERENCES public."Lessons" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_LessonProgressions_LessonId" ON public."LessonProgressions" ("LessonId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_LessonProgressions_StudentId_LessonId" ON public."LessonProgressions" ("StudentId", "LessonId");

CREATE TABLE IF NOT EXISTS public."QuizResults" (
    "Id" SERIAL PRIMARY KEY,
    "StudentId" varchar(450) NOT NULL,
    "QuizId" integer NOT NULL,
    "Score" double precision NOT NULL,
    "IsPassed" boolean NOT NULL,
    "AttemptDate" timestamp NOT NULL,
    CONSTRAINT "FK_QuizResults_Quizzes_QuizId" FOREIGN KEY ("QuizId") REFERENCES public."Quizzes" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_QuizResults_QuizId" ON public."QuizResults" ("QuizId");

CREATE TABLE IF NOT EXISTS public."Certificates" (
    "Id" SERIAL PRIMARY KEY,
    "StudentId" varchar(450) NOT NULL,
    "ModuleId" integer NOT NULL,
    "UniqueCode" varchar(100) NOT NULL,
    "IssueDate" timestamp NOT NULL,
    CONSTRAINT "FK_Certificates_Modules_ModuleId" FOREIGN KEY ("ModuleId") REFERENCES public."Modules" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Certificates_ModuleId" ON public."Certificates" ("ModuleId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Certificates_UniqueCode" ON public."Certificates" ("UniqueCode");

CREATE TABLE IF NOT EXISTS public."DiscussionThreads" (
    "Id" SERIAL PRIMARY KEY,
    "Title" varchar(200) NOT NULL,
    "StudentId" varchar(450) NOT NULL,
    "CreatedAt" timestamp NOT NULL,
    "IsResolved" boolean NOT NULL
);

CREATE TABLE IF NOT EXISTS public."DiscussionReplies" (
    "Id" SERIAL PRIMARY KEY,
    "DiscussionThreadId" integer NOT NULL,
    "AuthorId" varchar(450) NOT NULL,
    "Message" varchar(2000) NOT NULL,
    "CreatedAt" timestamp NOT NULL,
    CONSTRAINT "FK_DiscussionReplies_DiscussionThreads_DiscussionThreadId" FOREIGN KEY ("DiscussionThreadId") REFERENCES public."DiscussionThreads" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_DiscussionReplies_DiscussionThreadId" ON public."DiscussionReplies" ("DiscussionThreadId");

INSERT INTO public."Roles" ("Name")
SELECT role_name
FROM (VALUES ('etudiant'), ('enseignant'), ('coordinateur')) AS seed_roles(role_name)
ON CONFLICT ("Name") DO NOTHING;

SELECT setval(pg_get_serial_sequence('public."Roles"', 'Id'), COALESCE(MAX("Id"), 1), true) FROM public."Roles";
SELECT setval(pg_get_serial_sequence('public."Users"', 'Id'), COALESCE(MAX("Id"), 1), true) FROM public."Users";
SELECT setval(pg_get_serial_sequence('public."Quizzes"', 'Id'), COALESCE(MAX("Id"), 1), true) FROM public."Quizzes";
SELECT setval(pg_get_serial_sequence('public."Modules"', 'Id'), COALESCE(MAX("Id"), 1), true) FROM public."Modules";
SELECT setval(pg_get_serial_sequence('public."Lessons"', 'Id'), COALESCE(MAX("Id"), 1), true) FROM public."Lessons";
SELECT setval(pg_get_serial_sequence('public."Questions"', 'Id'), COALESCE(MAX("Id"), 1), true) FROM public."Questions";
SELECT setval(pg_get_serial_sequence('public."Options"', 'Id'), COALESCE(MAX("Id"), 1), true) FROM public."Options";
SELECT setval(pg_get_serial_sequence('public."Enrollments"', 'Id'), COALESCE(MAX("Id"), 1), true) FROM public."Enrollments";
SELECT setval(pg_get_serial_sequence('public."LessonProgressions"', 'Id'), COALESCE(MAX("Id"), 1), true) FROM public."LessonProgressions";
SELECT setval(pg_get_serial_sequence('public."QuizResults"', 'Id'), COALESCE(MAX("Id"), 1), true) FROM public."QuizResults";
SELECT setval(pg_get_serial_sequence('public."Certificates"', 'Id'), COALESCE(MAX("Id"), 1), true) FROM public."Certificates";
SELECT setval(pg_get_serial_sequence('public."DiscussionThreads"', 'Id'), COALESCE(MAX("Id"), 1), true) FROM public."DiscussionThreads";
SELECT setval(pg_get_serial_sequence('public."DiscussionReplies"', 'Id'), COALESCE(MAX("Id"), 1), true) FROM public."DiscussionReplies";
