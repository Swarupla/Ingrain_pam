import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdDeployModelComponent } from './ad-deploy-model.component';

describe('AdDeployModelComponent', () => {
  let component: AdDeployModelComponent;
  let fixture: ComponentFixture<AdDeployModelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AdDeployModelComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AdDeployModelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
