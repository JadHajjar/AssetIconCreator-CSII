import { ModRegistrar } from "cs2/modding";
import { ProgressPanel } from "mods/ProgressPanel/ProgressPanel";

const register: ModRegistrar = (moduleRegistry) => {
  moduleRegistry.append("Game", () => ProgressPanel(false));
  moduleRegistry.append("Editor", () => ProgressPanel(false));
};

export default register;
