SELECT u.UserName, COUNT(*) AS TotalAppeals
FROM Appeal a
JOIN AspNetUsers u ON a.PlayerId = u.Id
GROUP BY u.UserName
HAVING COUNT(*) > 1;