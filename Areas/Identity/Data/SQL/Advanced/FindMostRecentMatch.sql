SELECT u.UserName, 
       MAX(m.MatchDate) AS LastMatchDate
FROM AspNetUsers u
JOIN Match m ON u.Id = m.WhitePlayerId OR u.Id = m.BlackPlayerId
GROUP BY u.UserName
ORDER BY LastMatchDate DESC;