SELECT u.UserName, COUNT(*) AS TotalMatches
FROM AspNetUsers u
JOIN Match m ON u.Id = m.WhitePlayerId OR u.Id = m.BlackPlayerId
GROUP BY u.UserName
ORDER BY TotalMatches DESC;