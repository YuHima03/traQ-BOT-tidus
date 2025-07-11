CREATE TABLE IF NOT EXISTS `message_face_scores` (
    `message_id`                CHAR(36)        NOT NULL    PRIMARY KEY                 COMMENT 'message uuid',
    `user_id`                   CHAR(36)        NOT NULL                                COMMENT 'message author user uuid',
    `positive_phrase_count`     INT UNSIGNED    NOT NULL    DEFAULT 0,
    `negative_phrase_count`     INT UNSIGNED    NOT NULL    DEFAULT 0,
    `positive_reaction_count`   INT UNSIGNED    NOT NULL    DEFAULT 0,
    `negative_reaction_count`   INT UNSIGNED    NOT NULL    DEFAULT 0,
    `created_at`                DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP,
    `updated_at`                DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP   ON UPDATE CURRENT_TIMESTAMP
)   DEFAULT CHARSET=utf8mb4;
ALTER TABLE `message_face_scores` ADD INDEX `idx_user` (`user_id`);

CREATE TABLE `discord_webhooks` (
    `id`                CHAR(36)        NOT NULL    PRIMARY KEY,
    `post_url`          VARCHAR(255)    NULL        COMMENT 'Webhook URL',
    `user_id`           CHAR(36)        NOT NULL
    `is_enabled`        BOOLEAN         NOT NULL    DEFAULT 0,
    `notifies_on_flags` INT             NOT NULL    DEFAULT 0,
    `created_at`        DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP,
    `updated_at`        DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
)   DEFAULT CHARSET=utf8mb4;
ALTER TABLE `discord_webhooks` ADD INDEX (`user_id`);
ALTER TABLE `discord_webhooks` ADD INDEX (`is_enabled`);
