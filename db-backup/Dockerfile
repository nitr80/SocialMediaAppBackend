# PostgreSQL backup image: `pg_dump` (from the postgres image, matching the
# server major version) plus the AWS CLI v2 for uploading to S3 / Cloudflare R2.
FROM postgres:18

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl unzip \
    && curl -fsSL "https://awscli.amazonaws.com/awscli-exe-linux-$(uname -m).zip" -o /tmp/awscliv2.zip \
    && unzip -q /tmp/awscliv2.zip -d /tmp \
    && /tmp/aws/install \
    && rm -rf /tmp/aws /tmp/awscliv2.zip /var/lib/apt/lists/*

COPY db-backup/backup.sh /usr/local/bin/backup.sh
RUN chmod +x /usr/local/bin/backup.sh

USER postgres

ENTRYPOINT ["/usr/local/bin/backup.sh"]
