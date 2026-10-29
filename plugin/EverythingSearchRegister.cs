using EverythingSearch.Nodes;
using EverythingSearch.Services;

namespace EverythingSearch;

public class EverythingSearchRegister : RegisterBase<EverythingSearchRegister>
{
    public override void RegisterService(ContainerBuilder builder, IServiceCollection services)
    {
        var plugin = new EverythingSearchPlugin();

        builder.RegisterInstance(plugin)
            .As<IPlugin>()
            .Keyed<IPlugin>(EverythingSearchConst.PluginId)
            .As(plugin.GetType());

        builder.RegisterType<EverythingSearchScope>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<AudioSearchScope>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<ExeSearchScope>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<DocSearchScope>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<FolderSearchScope>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<VideoSearchScope>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<ArchiveSearchScope>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<ImageSearchScope>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<EverythingOpenNode>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<EverythingClientService>()
            .As<IEverythingClientService>()
            .SingleInstance();

        builder.RegisterType<EverythingStatusCallStrategy>()
            .As<IPluginCallStrategy>()
            .SingleInstance();
        builder.RegisterType<EverythingSearchCallStrategy>()
            .As<IPluginCallStrategy>()
            .SingleInstance();

        builder.Register<IFileIconCacheService>(_ =>
                new FileIconCacheService(() =>
                {
                    try
                    {
                        return plugin.Context?.Icons;
                    }
                    catch
                    {
                        return null;
                    }
                }))
            .SingleInstance();
    }
}
