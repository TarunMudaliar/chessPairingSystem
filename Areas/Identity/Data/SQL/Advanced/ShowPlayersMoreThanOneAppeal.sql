SELECT u.PlayerName, COUNT(*) AS TotalAppeals
FROM Appeal a
JOIN AspNetUsers u ON a.PlayerId = u.Id
GROUP BY u.PlayerName
HAVING COUNT(*) > 1;