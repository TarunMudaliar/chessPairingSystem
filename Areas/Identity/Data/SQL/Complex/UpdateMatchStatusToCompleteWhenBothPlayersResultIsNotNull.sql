UPDATE Match 
SET Status = 'Completed'
WHERE WhiteResult IS NOT NULL 
AND BlackResult IS NOT NULL
AND Status = 'Pending';