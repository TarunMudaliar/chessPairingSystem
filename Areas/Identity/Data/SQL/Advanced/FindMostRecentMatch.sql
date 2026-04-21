SELECT u.PlayerName, 
       CONVERT(varchar(19), MAX(m.MatchDate), 120) AS LastMatchDate
FROM AspNetUsers u
JOIN Match m ON u.Id = m.WhitePlayerId OR u.Id = m.BlackPlayerId
GROUP BY u.PlayerName
ORDER BY LastMatchDate DESC;