﻿       IDENTIFICATION DIVISION.
       PROGRAM-ID. PERFORM0.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 action PIC 9 VALUE 0.
          88 posibility VALUE 1, 2.
       PROCEDURE DIVISION.
           PERFORM UNTIL posibility
           DISPLAY "action = " NO ADVANCING
             ACCEPT action
           END-PERFORM
           DISPLAY "Right action !".
           STOP RUN.
       END PROGRAM PERFORM0.
