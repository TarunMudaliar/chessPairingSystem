SELECT u.PlayerName, COUNT(*) AS TotalMatches
FROM AspNetUsers u
JOIN Match m ON u.Id = m.WhitePlayerId OR u.Id = m.BlackPlayerId
GROUP BY u.PlayerName
ORDER BY TotalMatches DESC;