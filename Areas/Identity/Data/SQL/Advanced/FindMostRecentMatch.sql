SELECT u.PlayerName, 
       MAX(m.MatchDate) AS LastMatchDate
FROM AspNetUsers u
JOIN Match m ON u.Id = m.WhitePlayerId OR u.Id = m.BlackPlayerId
GROUP BY u.PlayerName
ORDER BY LastMatchDate DESC;