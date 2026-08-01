import { Button, FOCUS_DISABLED, Scrollable, Tooltip } from "cs2/ui";
import styles from "./ProgressPanel.module.scss";
import { bindValue, trigger, useValue } from "cs2/api";
import { useEffect, useState } from "react";
import { useLocalization } from "cs2/l10n";
import classNames from "classnames";
import mod from "mod.json";

const inProcess$ = bindValue<boolean>(mod.id, "InProcess");
const settingUp$ = bindValue<boolean>(mod.id, "SettingUp");
const progressText$ = bindValue<string>(mod.id, "ProgressText");
const prefabName$ = bindValue<string>(mod.id, "PrefabName");
const resultThumbnail$ = bindValue<string>(mod.id, "ResultThumbnail");
const cameraDebug$ = bindValue<boolean>(mod.id, "CameraDebug");
const saveThumbnails$ = bindValue<boolean>(mod.id, "SaveThumbnails");

const DISMISS_DURATION = 6000;
const DISMISS_TICK = 50;

export const ProgressPanel = (editor: boolean) => {
  const { translate } = useLocalization();
  const progressText = useValue(progressText$);
  const inProcess = useValue(inProcess$);
  const settingUp = useValue(settingUp$);
  const resultThumbnail = useValue(resultThumbnail$);
  const prefabName = useValue(prefabName$);
  const cameraDebug = useValue(cameraDebug$);
  const saveThumbnails = useValue(saveThumbnails$);

  const [hovered, setHovered] = useState(false);
  const [dismissProgress, setDismissProgress] = useState(0);

  useEffect(() => {
    if (!inProcess || !resultThumbnail) {
      setDismissProgress(0);
      return;
    }

    if (hovered) return;

    const interval = setInterval(
      () =>
        setDismissProgress((p) =>
          Math.min(1, p + DISMISS_TICK / DISMISS_DURATION),
        ),
      DISMISS_TICK,
    );

    return () => clearInterval(interval);
  }, [inProcess, resultThumbnail, hovered]);

  useEffect(() => {
    if (dismissProgress >= 1) {
      trigger(mod.id, "Dismiss");
    }
  }, [dismissProgress]);

  if (!inProcess) return <></>;

  return (
    <div
      className={classNames(
        styles.panel,
        settingUp && !cameraDebug && styles.fullScreen,
        resultThumbnail && styles.expanded,
      )}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
    >
      <div className={styles.header}>Asset Icon Creator</div>
      <div className={styles.headerBorder}>
        <div style={{ width: `${dismissProgress * 100}%` }} />
      </div>
      <div className={styles.content}>
        <div />
        {resultThumbnail && <img src={resultThumbnail} />}
        <div className={styles.progress}>
          {!resultThumbnail && <div className={styles.spinner} />}
          <span>{progressText}</span>
        </div>
        {resultThumbnail && (
          <div className={styles.prefabName}>{prefabName}</div>
        )}
        <div className={styles.footer}>
          {resultThumbnail && (
            <>
              {saveThumbnails && (
                <span className={styles.savedLabel}>
                  Icon saved to your icons folder
                </span>
              )}
              <Tooltip
                tooltip={
                  saveThumbnails
                    ? "Show icon in Explorer"
                    : "Save icon & show it in Explorer"
                }
              >
                <Button
                  className={styles.saveButton}
                  focusKey={FOCUS_DISABLED}
                  variant="icon"
                  onSelect={() =>
                    trigger(mod.id, saveThumbnails ? "ShowIcon" : "SaveIcon")
                  }
                >
                  <img
                    src={
                      saveThumbnails
                        ? "Media/Glyphs/ViewInfo.svg"
                        : "Media/Editor/Save.svg"
                    }
                  />
                </Button>
              </Tooltip>
            </>
          )}
        </div>
      </div>
    </div>
  );
};
