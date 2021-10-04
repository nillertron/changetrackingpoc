POC Antagelser:
Der eksistere 1-mange databaser der synkroniseres fra
Der eksistere 1 database der synkroniseres til
Der angvies tenant id i programmet
Der angives table(s) der ønskes synkronisering, attribut navnene skal være de samme. 
På target databasen, forventes der at være en attribut der hedder TenantId, den vil sammen med primær nøglen fra afsender
tabellen danne primær nøglen i den multi tenant db'en
Databaserne man synkronisre fra, skal have change tracking slået til.



