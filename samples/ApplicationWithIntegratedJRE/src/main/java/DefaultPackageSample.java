
public class DefaultPackageSample {

	public DefaultPackageSample() {
		System.out.println(this.getClass().getCanonicalName() + " constructor called");
	}
	
	public String getExecutablePath() {
		String javaExecutablePath = ProcessHandle.current()
			    .info()
			    .command()
			    .orElseThrow();
		return javaExecutablePath;
	}

}
