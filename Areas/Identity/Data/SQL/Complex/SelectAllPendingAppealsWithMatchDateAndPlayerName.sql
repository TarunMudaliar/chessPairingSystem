SELECT a.AppealId,
       u.PlayerName AS SubmittedBy,
       a.Message,
       a.Status,
       CONVERT(varchar(19), m.MatchDate, 120) AS MatchDate
FROM Appeal a
JOIN AspNetUsers u ON a.PlayerId = u.Id
JOIN Match m ON a.GameId = m.GameId
WHERE a.Status = 'Pending';