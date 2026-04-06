#!/bin/bash
# Backup script for WolfBlockchain local data files
# Creates a timestamped snapshot of blockchain.json and wallets.json
# Usage: ./backup.sh [data_dir] [backup_root_dir]
#   data_dir        – directory containing blockchain.json / wallets.json (default: ./data)
#   backup_root_dir – directory where backup snapshots are stored   (default: ./backups)

set -euo pipefail

DATA_DIR="${1:-data}"
BACKUP_ROOT="${2:-backups}"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_DIR="$BACKUP_ROOT/backup-$TIMESTAMP"

echo "💾 WolfBlockchain Backup"
echo "========================"
echo "Source  : $DATA_DIR"
echo "Target  : $BACKUP_DIR"
echo ""

# Validate source exists
if [ ! -d "$DATA_DIR" ]; then
    echo "❌ Data directory '$DATA_DIR' not found. Nothing to back up."
    exit 1
fi

# Ensure at least one of the expected files is present
if [ ! -f "$DATA_DIR/blockchain.json" ] && [ ! -f "$DATA_DIR/wallets.json" ]; then
    echo "❌ No blockchain.json or wallets.json found in '$DATA_DIR'."
    exit 1
fi

# Create backup directory
mkdir -p "$BACKUP_DIR"

# Copy available data files
COPIED=0
for FILE in blockchain.json wallets.json; do
    SRC="$DATA_DIR/$FILE"
    if [ -f "$SRC" ]; then
        cp "$SRC" "$BACKUP_DIR/$FILE"
        echo "✅ Copied $FILE"
        COPIED=$((COPIED + 1))
    else
        echo "⚠️  $FILE not found – skipped"
    fi
done

# Write a manifest
MANIFEST="$BACKUP_DIR/MANIFEST"
{
    echo "timestamp=$TIMESTAMP"
    echo "source=$DATA_DIR"
    echo "files_backed_up=$COPIED"
} > "$MANIFEST"

echo ""
echo "========================"
echo "✅ Backup complete: $BACKUP_DIR ($COPIED file(s))"
exit 0
