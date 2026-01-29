-- ================================================
-- QUICK FIX: Enable your seller account
-- ================================================

-- Problem detected from debug output:
-- - Email: vendeur@test.com
-- - User ID: 1
-- - Status: NOT CERTIFIED (is_certified = 0)
-- - This blocks login even if password is correct

-- Solution: Certify the seller account

UPDATE vendeur 
SET is_certified = 1 
WHERE id_user = 1;

-- Verify the change:
SELECT 
    u.email,
    v.is_certified,
    CASE WHEN v.is_certified = 1 THEN '? Can login' ELSE '? Blocked' END as status
FROM utilisateur u
JOIN vendeur v ON u.id_user = v.id_user
WHERE u.email = 'vendeur@test.com';

-- Expected result:
-- email              | is_certified | status
-- -------------------+--------------+----------------
-- vendeur@test.com   | 1            | ? Can login
