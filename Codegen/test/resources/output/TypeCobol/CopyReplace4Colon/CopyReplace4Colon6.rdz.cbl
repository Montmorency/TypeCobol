﻿       IDENTIFICATION DIVISION.
       PROGRAM-ID. CPYRPL4C.
      
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 xxxENT2. COPY YxxxENT REPLACING ==::== BY ====.
       01 xxxENT3. COPY YxxxENT REPLACING ==::== BY ==S==.
       PROCEDURE DIVISION.
           MOVE 'A' TO xxxENTS-FCT01-Var1.
       END PROGRAM CPYRPL4C.
