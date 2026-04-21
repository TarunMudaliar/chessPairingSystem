SELECT a.AppealId,
       u.PlayerName AS SubmittedBy,
       a.Message,
       a.AdminResponse,
       CONVERT(varchar(19), a.SubmittedAt, 120) AS SubmittedAt
FROM Appeal a
JOIN AspNetUsers u ON a.PlayerId = u.Id
WHERE a.Status = 'Resolved'
