async function getVersionFromSemanticRelease() {
    const { default: semanticRelease } = await import('semantic-release');
    return await semanticRelease(
        {
            dryRun: true,
            ci: false,
        },
        {
            stdout: process.stderr,
        }
    );
}

export async function main() {
    const result = await getVersionFromSemanticRelease();
    if (result) {
        process.stdout.write(result.nextRelease.version);
    }
}

main().catch((e) => {
    console.error(e);
    process.exit(1);
});
