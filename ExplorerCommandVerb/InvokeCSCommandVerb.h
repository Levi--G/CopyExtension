#pragma once

#include "Dll.h"
#include "ShellHelpers.h"
#include "Clipboard.h"

class InvokeCSCommandVerb : public IExplorerCommand
{
public:
	InvokeCSCommandVerb() : _cRef(1)
	{
		DllAddRef();
	}

	// IUnknown
	virtual IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv) override = 0;

	IFACEMETHODIMP_(ULONG) AddRef() override
	{
		return InterlockedIncrement(&_cRef);
	}

	IFACEMETHODIMP_(ULONG) Release() override
	{
		long cRef = InterlockedDecrement(&_cRef);
		if (!cRef)
		{
			delete this;
		}
		return cRef;
	}

	// IExplorerCommand
	virtual IFACEMETHODIMP GetTitle(IShellItemArray* /* psiItemArray */, LPWSTR* ppszName) override = 0;

	IFACEMETHODIMP GetIcon(IShellItemArray* /* psiItemArray */, LPWSTR* ppszIcon) override
	{
		// the icon ref ("dll,-<resid>") is provied here, in this case none is provieded
		*ppszIcon = NULL;
		return E_NOTIMPL;
	}

	IFACEMETHODIMP GetToolTip(IShellItemArray* /* psiItemArray */, LPWSTR* ppszInfotip) override
	{
		// tooltip provided here, in this case none is provieded
		*ppszInfotip = NULL;
		return E_NOTIMPL;
	}

	virtual IFACEMETHODIMP GetCanonicalName(GUID* pguidCommandName) override = 0;

	// compute the visibility of the verb here, respect "fOkToBeSlow" if this is slow (does IO for example)
	// when called with fOkToBeSlow == FALSE return E_PENDING and this object will be called
	// back on a background thread with fOkToBeSlow == TRUE 
	virtual IFACEMETHODIMP GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) override
	{
		if (DoesClipboardHaveData()) {
			*pCmdState = ECS_ENABLED;
		}
		else {
			*pCmdState = ECS_HIDDEN;
		}
		return S_OK;
	}

	IFACEMETHODIMP Invoke(IShellItemArray* psia, IBindCtx* /*pbc*/) override;

	IFACEMETHODIMP GetFlags(EXPCMDFLAGS* pFlags)
	{
		*pFlags = ECF_DEFAULT;
		return S_OK;
	}

	IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** ppEnum) override
	{
		*ppEnum = NULL;
		return E_NOTIMPL;
	}

protected:
	virtual ~InvokeCSCommandVerb()
	{
		DllRelease();
	}

	virtual CommandType GetVerbType() = 0;

private:
	static DWORD __stdcall s_ThreadProc(void* pv);

	long _cRef;
};

template<typename T>
class GenericInvokeCSCommandVerb : public InvokeCSCommandVerb
{
public:
	// Inherited via InvokeCSCommandVerb
	IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv) override
	{
		static const QITAB qit[] =
		{
			QITABENT(T, IExplorerCommand),
			{ 0 },
		};
		return QISearch(this, qit, riid, ppv);
	}

	IFACEMETHODIMP GetCanonicalName(GUID* pguidCommandName) override
	{
		*pguidCommandName = __uuidof(T);
		return S_OK;
	}
};