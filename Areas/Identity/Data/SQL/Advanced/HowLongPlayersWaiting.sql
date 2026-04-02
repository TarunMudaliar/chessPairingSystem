SELECT u.PlayerName,
       c.CategoryName,
       mq.TimeJoined,
       mq.Location,
       mq.ScheduledTime,
       DATEDIFF(MINUTE, mq.TimeJoined, GETDATE()) AS MinutesWaiting
FROM MatchQueue mq
JOIN AspNetUsers u ON mq.PlayerId = u.Id
JOIN Category c ON u.CategoryId = c.CategoryId
ORDER BY mq.TimeJoined ASC;