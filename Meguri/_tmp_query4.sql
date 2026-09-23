\encoding UTF8
SELECT i."Id", i."Name", i."IsPublic", i."IsSexual", i."IsViolence"
FROM "Images" i
JOIN "ImageTags" it ON it."ImageId" = i."Id"
WHERE it."TagConceptId" = 150;

SELECT p."Id", p."Name", p."IsPublic"
FROM "Posts" p
JOIN "PostTags" pt ON pt."PostId" = p."Id"
WHERE pt."TagConceptId" = 150;
