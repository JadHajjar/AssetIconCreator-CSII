import { Button, FOCUS_DISABLED, Scrollable, Tooltip } from "cs2/ui";
import styles from "./ProgressPanel.module.scss";
import { bindValue, useValue } from "cs2/api";
import { useEffect, useState } from "react";
import { useLocalization } from "cs2/l10n";
import classNames from "classnames";
import mod from "mod.json";

const inProcess$ = bindValue<boolean>(mod.id, "InProcess");
const settingUp$ = bindValue<boolean>(mod.id, "SettingUp");
const progressText$ = bindValue<string>(mod.id, "ProgressText");
const resultThumbnail$ = bindValue<string>(mod.id, "ResultThumbnail");

export const ProgressPanel = (editor: boolean) => {
  const { translate } = useLocalization();
  const progressText = useValue(progressText$);
  const inProcess = useValue(inProcess$);
  const settingUp = useValue(settingUp$);
  const resultThumbnail = useValue(resultThumbnail$);

  if (!inProcess) return <></>;

  return (
    <div className={classNames(styles.panel, settingUp && styles.fullScreen, resultThumbnail && styles.expanded)}>
      <div className={styles.header}>Asset Icon Creator</div>
      <div className={styles.content}>
        {resultThumbnail && <img src={resultThumbnail} />}
        <span>{progressText}</span>
      </div>
    </div>
  );
};
