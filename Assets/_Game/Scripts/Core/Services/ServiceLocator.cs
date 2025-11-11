using System;
using System.Collections.Generic;
using UnityEngine;

public class ServiceLocator : Singleton<ServiceLocator>
{
    private Dictionary<Type, MonoBehaviour> m_Services = new Dictionary<Type, MonoBehaviour>();

    public void Add<TType>(MonoBehaviour Service) where TType : MonoBehaviour
    {
        MonoBehaviour ExistingService;
        if (m_Services.TryGetValue(typeof(TType), out ExistingService))
        {
            Destroy(ExistingService.gameObject);
        }

        Service.transform.SetParent(transform);
        m_Services[typeof(TType)] = Service;
    }

    public void Add<TType, TService>()
        where TType : MonoBehaviour
        where TService : MonoBehaviour
    {
        MonoBehaviour ExistingService;
        if (m_Services.TryGetValue(typeof(TType), out ExistingService))
        {
            Destroy(ExistingService.gameObject);
        }

        CreateService<TType, TService>();
    }

    public TType Get<TType>() where TType : MonoBehaviour
    {
        MonoBehaviour Service;
        if (!m_Services.TryGetValue(typeof(TType), out Service))
        {
            Service = CreateService<TType, TType>();
        }

        return (TType)Service;
    }

    private TService CreateService<TType, TService>()
        where TType : MonoBehaviour
        where TService : MonoBehaviour
    {
        // Check if this service exists 
        TType ExistingService = FindFirstObjectByType<TType>();
        if (ExistingService)
        {
            // Take this service and return if that's right type
            var ExistingAsNeeded = ExistingService as TService;
            if (ExistingAsNeeded)
            {
                ExistingAsNeeded.transform.parent = transform;
                ExistingAsNeeded.name = typeof(TService).Name;
                m_Services[typeof(TType)] = ExistingAsNeeded;
                return ExistingAsNeeded;
            }

            // So, delete previous service
            Destroy(ExistingService.gameObject);
        }

        // Create new service
        GameObject ServiceObject = new GameObject();
        ServiceObject.name = typeof(TService).Name;
        ServiceObject.transform.parent = transform;

        var NewService = ServiceObject.AddComponent<TService>();
        m_Services[typeof(TType)] = NewService;

        return NewService;
    }
}
