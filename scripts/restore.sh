#!/bin/bash
# Restore script for WolfBlockchain local data files
# Restores blockchain.json and wallets.json from a backup snapshot
# Usage: ./restore.sh [backup_dir] [data_dir]
#   backup_dir – path to the backup snapshot to restore from
#                (omit to restore from the most recent backup under ./backups)
#   data_dir   – target directory for restored files (default: ./data)

set -euo pipefail

BACKUP_ROOT="backups"
DATA_DIR="${2:-data}"

# ── Resolve backup directory ────────────────────────────────────────────────
if [ -n "${1:-}" ]; then
    BACKUP_DIR="$1"
else
    # Pick the most recent backup automatically
    if [ ! -d "$BACKUP_ROOT" ]; then
        echo "❌ No backups directory found and no backup path provided."
        echo "   Run scripts/backup.sh first to create a backup."
        exit 1
    fi
    BACKUP_DIR=$(find "$BACKUP_ROOT" -mindepth 1 -maxdepth 1 -type d | sort | tail -n 1)
    if [ -z "$BACKUP_DIR" ]; then
        echo "❌ No backup snapshots found in '$BACKUP_ROOT'."
        echo "   Run scripts/backup.sh first to create a backup."
        exit 1
    fi
fi

echo "🔄 WolfBlockchain Restore"
echo "========================="
echo "Backup source : $BACKUP_DIR"
echo "Restore target: $DATA_DIR"
echo ""

# ── Validate backup directory ───────────────────────────────────────────────
if [ ! -d "$BACKUP_DIR" ]; then
    echo "❌ Backup directory '$BACKUP_DIR' does not exist."
    exit 1
fi

if [ ! -f "$BACKUP_DIR/blockchain.json" ] && [ ! -f "$BACKUP_DIR/wallets.json" ]; then
    echo "❌ '$BACKUP_DIR' contains neither blockchain.json nor wallets.json."
    exit 1
fi

# ── Safety snapshot of current data ─────────────────────────────────────────
if [ -d "$DATA_DIR" ]; then
    SAFETY_DIR="$BACKUP_ROOT/pre-restore-$(date +%Y%m%d-%H%M%S)"
    mkdir -p "$SAFETY_DIR"
    SAFETY_COPIED=0
    for FILE in blockchain.json wallets.json; do
        if [ -f "$DATA_DIR/$FILE" ]; then
            cp "$DATA_DIR/$FILE" "$SAFETY_DIR/$FILE"
            SAFETY_COPIED=$((SAFETY_COPIED + 1))
        fi
    done
    if [ "$SAFETY_COPIED" -gt 0 ]; then
        echo "💾 Safety snapshot of current data saved to $SAFETY_DIR"
    fi
fi

# ── Prepare target directory ─────────────────────────────────────────────────
mkdir -p "$DATA_DIR"

# ── Restore files ────────────────────────────────────────────────────────────
RESTORED=0
FAILED=0

for FILE in blockchain.json wallets.json; do
    SRC="$BACKUP_DIR/$FILE"
    DST="$DATA_DIR/$FILE"

    if [ ! -f "$SRC" ]; then
        echo "⚠️  $FILE not present in backup – skipped"
        continue
    fi

    # Validate JSON before overwriting
    if ! python3 -c "import json,sys; json.load(open('$SRC'))" 2>/dev/null; then
        echo "❌ $FILE in backup is not valid JSON – skipped to protect data integrity"
        FAILED=$((FAILED + 1))
        continue
    fi

    cp "$SRC" "$DST"
    echo "✅ Restored $FILE"
    RESTORED=$((RESTORED + 1))
done

echo ""
echo "========================="

if [ "$FAILED" -gt 0 ]; then
    echo "❌ Restore completed with $FAILED error(s). Check messages above."
    exit 1
fi

if [ "$RESTORED" -eq 0 ]; then
    echo "⚠️  No files were restored."
    exit 1
fi

echo "✅ Restore complete: $RESTORED file(s) restored to '$DATA_DIR'"
exit 0
